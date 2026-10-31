using Fusi.Api.Auth.Controllers;
using Fusi.Api.Auth.Models;
using Fusi.Api.Auth.Services;
using Fusi.DbManager.PgSql;
using MessagingApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using NpgsqlTypes;
using Pleiades.Search;
using Pleiades.Search.PgSql;
using PleiadesApi.Services;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;
using Serilog.Sinks.PostgreSQL.ColumnWriters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace PleiadesApi;

/// <summary>
/// Main program.
/// </summary>
public static class Program
{
    // startup log file name, Serilog is configured later via appsettings.json
    private const string STARTUP_LOG_NAME = "startup.log";

    private static readonly string[] _secretKeyParts =
        ["KEY", "PASSWORD", "SECRET", "TOKEN", "CONNECTIONSTRING"];

    // the last time a rate limit notification was sent (ticks)
    private static long _lastLimitNotification;

    /// <summary>
    /// Dumps the environment variables in the log, masking secrets.
    /// </summary>
    private static void DumpEnvironmentVars()
    {
        Log.Information("ENVIRONMENT VARIABLES:");
        IDictionary dct = Environment.GetEnvironmentVariables();
        foreach (string key in dct.Keys.Cast<object>()
            .Select(k => k.ToString()!).Order())
        {
            bool secret = _secretKeyParts.Any(p =>
                key.Contains(p, StringComparison.OrdinalIgnoreCase));
            Log.Information("{Key} = {Value}", key, secret ? "***" : dct[key]);
        }
    }

    #region Messaging
    private static void ConfigureMessagingServices(IServiceCollection services,
        IConfiguration config)
    {
        // configuration sections
        // https://andrewlock.net/adding-validation-to-strongly-typed-configuration-objects-in-asp-net-core/
        services.Configure<MessagingOptions>(config.GetSection("Messaging"));
        services.Configure<DotNetMailerOptions>(config.GetSection("Mailer"));

        // explicitly register the settings object by delegating to the IOptions object
        services.AddSingleton(resolver =>
            resolver.GetRequiredService<IOptions<MessagingOptions>>().Value);
        services.AddSingleton(resolver =>
            resolver.GetRequiredService<IOptions<DotNetMailerOptions>>().Value);

        // you can use another mailer service here
        services.AddScoped<IMailerService, DotNetMailerService>();
        services.AddScoped<IMessageBuilderService,
            FileMessageBuilderService>();
    }
    #endregion

    #region CORS
    private static void ConfigureCorsServices(IServiceCollection services,
        IConfiguration config)
    {
        string[] origins = ["http://localhost:4200"];

        IConfigurationSection section = config.GetSection("AllowedOrigins");
        if (section.Exists())
        {
            origins = section.AsEnumerable()
                .Where(p => !string.IsNullOrEmpty(p.Value))
                .Select(p => p.Value).ToArray()!;
        }

        services.AddCors(o => o.AddPolicy("CorsPolicy", builder =>
        {
            builder.AllowAnyMethod()
                .AllowAnyHeader()
                // https://github.com/aspnet/SignalR/issues/2110 for AllowCredentials
                .AllowCredentials()
                .WithOrigins(origins)
                // let clients read the header telling that a JWT expired
                .WithExposedHeaders("Token-Expired");
        }));
    }
    #endregion

    #region Rate limiter
    private static async Task NotifyLimitExceededToRecipients(IConfiguration config,
        IHostEnvironment hostEnvironment)
    {
        // mailer must be enabled
        if (!config.GetValue<bool>("Mailer:IsEnabled"))
        {
            Log.Information("Mailer not enabled");
            return;
        }

        // notify at most once per time window, so that a flood of rejected
        // requests does not become a flood of email messages
        int minutes = Math.Max(1,
            config.GetValue("RateLimit:NotificationIntervalMinutes", 60));
        long now = DateTime.UtcNow.Ticks;
        long last = Interlocked.Read(ref _lastLimitNotification);
        if (now - last < TimeSpan.FromMinutes(minutes).Ticks
            || Interlocked.CompareExchange(ref _lastLimitNotification,
                now, last) != last)
        {
            return;
        }

        // recipients must be set
        IConfigurationSection recSection = config.GetSection("Mailer:Recipients");
        if (!recSection.Exists()) return;
        string[] recipients = [.. recSection.AsEnumerable()
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => p.Value!)];
        if (recipients.Length == 0)
        {
            Log.Information("No recipients for limit notification");
            return;
        }

        // build message
        MessagingOptions msgOptions = new();
        config.GetSection("Messaging").Bind(msgOptions);
        FileMessageBuilderService messageBuilder = new(
            msgOptions,
            hostEnvironment);

        Message? message = messageBuilder.BuildMessage("rate-limit-exceeded",
            new Dictionary<string, string>()
            {
                ["EventTime"] = DateTime.UtcNow.ToString(
                    "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            });
        if (message == null)
        {
            Log.Warning("Unable to build limit notification message");
            return;
        }

        // send message to all the recipients
        DotNetMailerOptions mailerOptions = new();
        config.GetSection("Mailer").Bind(mailerOptions);
        DotNetMailerService mailer = new(mailerOptions);

        foreach (string recipient in recipients)
        {
            try
            {
                Log.Information("Sending rate email message");
                await mailer.SendEmailAsync(recipient, "Test Recipient", message);
                Log.Information("Email message sent");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error sending rate limit notification");
            }
        }
    }

    private static void ConfigureRateLimiterService(IServiceCollection services,
        IConfiguration config, IHostEnvironment hostEnvironment)
    {
        // nope if Disabled
        IConfigurationSection limit = config.GetSection("RateLimit");
        if (limit.GetValue("IsDisabled", false))
        {
            Log.Information("Rate limiter is disabled");
            return;
        }

        // PermitLimit (10)
        int permit = limit.GetValue("PermitLimit", 10);
        if (permit < 1) permit = 10;

        // QueueLimit (0)
        int queue = limit.GetValue("QueueLimit", 0);

        // TimeWindow (00:01:00 = HH:MM:SS)
        string? windowText = limit.GetValue<string>("TimeWindow");
        if (string.IsNullOrEmpty(windowText) || !TimeSpan.TryParse(windowText,
            CultureInfo.InvariantCulture, out TimeSpan window))
        {
            window = TimeSpan.FromMinutes(1);
        }

        Log.Information("Configuring rate limiter: " +
            "limit={PermitLimit}, queue={QueueLimit}, window={Window}",
            permit, queue, window);

        // https://blog.maartenballiauw.be/post/2022/09/26/aspnet-core-rate-limiting-middleware.html
        // default = 10 requests per minute, per authenticated username,
        // or IP address if not authenticated.
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter
                .Create<HttpContext, string>(httpContext =>
                {
                    string key = httpContext.User.Identity?.Name
                        ?? httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: key,
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = permit,
                            QueueLimit = queue,
                            Window = window
                        });
                });

            options.OnRejected = async (context, token) =>
            {
                Log.Warning("Rate limit exceeded");

                // 429 too many requests
                context.HttpContext.Response.StatusCode =
                    StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                // notify without delaying the response
                _ = NotifyLimitExceededToRecipients(config, hostEnvironment);

                // ret JSON with error
                string error = context.Lease.TryGetMetadata(
                    MetadataName.RetryAfter, out var retryAfter)
                    ? "Too many requests. Please try again after " +
                      $"{Math.Ceiling(retryAfter.TotalMinutes)} minute(s)."
                    : "Too many requests. Please try again later.";
                await context.HttpContext.Response.WriteAsync(
                    JsonSerializer.Serialize(new { error }),
                    cancellationToken: token);
            };
        });
    }
    #endregion

    #region Auth
    private static void ConfigureAuthServices(IServiceCollection services,
        IConfiguration config)
    {
        // Auth connection string defaults to Default when not set
        string cs = DbConnectionStrings.Build(
            DbConnectionStrings.GetTemplate(config, "Auth"),
            config.GetValue<string>("DatabaseNames:Auth") ?? "pleiades");
        Log.Information("Auth database: {Database}",
            DbConnectionStrings.Describe(cs));
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(cs));

        services.AddIdentity<NamedUser, IdentityRole>(options =>
            {
                // see IdentityOptions for password, lockout, user policies
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // JWT authentication, configured from the Jwt section, i.e. the
        // same options used to issue tokens: Jwt:SecureKey (at least 32
        // chars), Jwt:Issuer, Jwt:Audience, and the optional
        // Jwt:ExpirationMinutes and Jwt:ClockSkewSeconds
        services.AddJwtBearerAuthentication(config);
#if DEBUG
        // use to show more information when troubleshooting JWT issues
        IdentityModelEventSource.ShowPII = true;
#endif

        // user repository service
        services.AddScoped<IUserRepository<NamedUser>,
            UserRepository<NamedUser, IdentityRole>>();
    }
    #endregion

    #region Logging
    private static bool IsAuditEnabledFor(IConfiguration config, string key,
        bool defaultValue = false)
    {
        return config.GetValue<bool?>($"Auditing:{key}") ?? defaultValue;
    }

    private static void ConfigurePostgreLogging(HostBuilderContext context,
        LoggerConfiguration loggerConfiguration)
    {
        string? cs = context.Configuration.GetConnectionString("PostgresLog");
        if (string.IsNullOrEmpty(cs))
        {
            Console.WriteLine("Postgres log connection string not found");
            return;
        }

        Regex dbRegex = new("Database=(?<n>[^;]+);?");
        Match m = dbRegex.Match(cs);
        if (!m.Success)
        {
            Console.WriteLine("Postgres log connection string not valid");
            return;
        }
        string cst = dbRegex.Replace(cs, "Database={0};");
        string dbName = m.Groups["n"].Value;
        PgSqlDbManager mgr = new(cst);
        if (!mgr.Exists(dbName))
        {
            Console.WriteLine($"Creating log database {dbName}...");
            mgr.CreateDatabase(dbName, "", null);
        }

        IDictionary<string, ColumnWriterBase> columnWriters =
            new Dictionary<string, ColumnWriterBase>
        {
            { "message", new RenderedMessageColumnWriter(
                NpgsqlDbType.Text) },
            { "message_template", new MessageTemplateColumnWriter(
                NpgsqlDbType.Text) },
            { "level", new LevelColumnWriter(
                true, NpgsqlDbType.Varchar) },
            { "raise_date", new TimestampColumnWriter(
                NpgsqlDbType.TimestampTz) },
            { "exception", new ExceptionColumnWriter(
                NpgsqlDbType.Text) },
            { "properties", new LogEventSerializedColumnWriter(
                NpgsqlDbType.Jsonb) },
            { "props_test", new PropertiesColumnWriter(
                NpgsqlDbType.Jsonb) },
            { "machine_name", new SinglePropertyColumnWriter(
                "MachineName", PropertyWriteMethod.ToString,
                NpgsqlDbType.Text, "l") }
        };

        loggerConfiguration
            .WriteTo.PostgreSQL(cs, "log", columnWriters,
            needAutoCreateTable: true);
    }

    private static void ConfigureLogger(WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((hostingContext, loggerConfiguration) =>
        {
            // EF logs each command as information: configuration (e.g.
            // Serilog:MinimumLevel:Override) can override this
            loggerConfiguration.MinimumLevel.Override(
                "Microsoft.EntityFrameworkCore", LogEventLevel.Warning);

            // https://github.com/serilog/serilog-settings-configuration
            loggerConfiguration.ReadFrom.Configuration(
                hostingContext.Configuration);

            if (IsAuditEnabledFor(hostingContext.Configuration, "File"))
            {
                Console.WriteLine("Logging to file enabled");
                loggerConfiguration.WriteTo.File("pleiades.log",
                    rollingInterval: RollingInterval.Day);
            }

            if (IsAuditEnabledFor(hostingContext.Configuration, "Postgres"))
            {
                Console.WriteLine("Logging to Postgres enabled");
                ConfigurePostgreLogging(hostingContext, loggerConfiguration);
            }

            // console is enabled unless explicitly disabled, so that the
            // log is available via docker logs
            if (IsAuditEnabledFor(hostingContext.Configuration, "Console",
                true))
            {
                Console.WriteLine("Logging to console enabled");
                loggerConfiguration.WriteTo.Console();
            }
        });
    }
    #endregion

    private static void ConfigureServices(IServiceCollection services,
        IConfiguration config, IHostEnvironment hostEnvironment)
    {
        // security
        ConfigureCorsServices(services, config);
        ConfigureRateLimiterService(services, config, hostEnvironment);
        ConfigureAuthServices(services, config);

        // proxy
        services.AddHttpClient();
        services.AddResponseCaching();

        // API controllers, with camel-case JSON and enums as strings
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });
        services.AddProblemDetails();

        // framework services
        // IMemoryCache: https://docs.microsoft.com/en-us/aspnet/core/performance/caching/memory
        services.AddMemoryCache();

        // messaging
        ConfigureMessagingServices(services, config);

        // logging
        services.AddSingleton(sp =>
        {
            ILoggerFactory factory = sp.GetRequiredService<ILoggerFactory>();
            return factory.CreateLogger("Logger");
        });

        // Pleiades search: stateless and thread-safe (each call uses its own
        // pooled connection), so it is a singleton
        string dataCs = DbConnectionStrings.Build(
            DbConnectionStrings.GetTemplate(config, "Default"),
            config.GetValue<string>("DatabaseNames:Data") ?? "pleiades");
        services.AddSingleton<IQuickSearch>(_ => new PgSqlQuickSearch(dataCs));

        // open API
        services.AddOpenApi();
    }

    /// <summary>
    /// Main entry point.
    /// </summary>
    /// <param name="args">The arguments.</param>
    public static async Task<int> Main(string[] args)
    {
        // early startup logging to ensure we catch any exceptions
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
#if DEBUG
            .WriteTo.File(STARTUP_LOG_NAME, rollingInterval: RollingInterval.Day)
#endif
            .CreateLogger();

        try
        {
            Log.Information("Starting Pleiades API host");
            DumpEnvironmentVars();

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            ConfigureLogger(builder);

            // appsettings.json, appsettings.{Environment}.json, environment
            // variables (e.g. Jwt__SecureKey), command line arguments
            IConfiguration config = builder.Configuration;
            ConfigureServices(builder.Services, config, builder.Environment);

            WebApplication app = builder.Build();

            // seed database (via Services/HostSeedExtension)
            await app.SeedAsync();

            // forward headers for use with an eventual reverse proxy
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor
                    | ForwardedHeaders.XForwardedProto
            });

            // development or production
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // unhandled exceptions become problem details (500)
                app.UseExceptionHandler();
                if (config.GetValue<bool>("Server:UseHSTS"))
                {
                    Console.WriteLine("HSTS: yes");
                    app.UseHsts();
                }
                else
                {
                    Console.WriteLine("HSTS: no");
                }
            }
            app.UseStatusCodePages();

            // HTTPS redirection
            if (config.GetValue<bool>("Server:UseHttpsRedirection"))
            {
                Console.WriteLine("HttpsRedirection: yes");
                app.UseHttpsRedirection();
            }
            else
            {
                Console.WriteLine("HttpsRedirection: no");
            }

            // CORS
            app.UseCors("CorsPolicy");
            // authentication (before the rate limiter, which partitions
            // requests by user name when authenticated)
            app.UseAuthentication();
            app.UseAuthorization();
            // rate limiter
            if (!config.GetValue<bool>("RateLimit:IsDisabled"))
                app.UseRateLimiter();
            // proxy
            app.UseResponseCaching();

            // map controllers and Scalar API
            app.MapControllers();
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options.WithTitle("Pleiades API")
                       .AddPreferredSecuritySchemes("Bearer");
            });

            Log.Information("Running API");
            await app.RunAsync();

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Pleiades API host terminated unexpectedly");
            Debug.WriteLine(ex.ToString());
            Console.WriteLine(ex.ToString());
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
