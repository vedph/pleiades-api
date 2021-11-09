using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Pleiades.Search;
using Pleiades.Search.PgSql;
using PleiadesApi.Models;
using PleiadesApi.Services;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace PleiadesApi
{
    /// <summary>
    /// Startup class.
    /// </summary>
    public sealed class Startup
    {
        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Gets the host environment.
        /// </summary>
        public IHostEnvironment HostEnvironment { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="environment">The environment.</param>
        public Startup(IConfiguration configuration, IHostEnvironment environment)
        {
            Configuration = configuration;
            HostEnvironment = environment;
        }

        private void ConfigureCorsServices(IServiceCollection services)
        {
            string[] origins = new[] { "http://localhost:4200" };

            IConfigurationSection section = Configuration.GetSection("AllowedOrigins");
            if (section.Exists())
            {
                origins = section.AsEnumerable()
                    .Where(p => !string.IsNullOrEmpty(p.Value))
                    .Select(p => p.Value).ToArray();
            }

            services.AddCors(o => o.AddPolicy("CorsPolicy", builder =>
            {
                builder.AllowAnyMethod()
                    .AllowAnyHeader()
                    // https://github.com/aspnet/SignalR/issues/2110 for AllowCredentials
                    .AllowCredentials()
                    .WithOrigins(origins);
            }));
        }

        private void ConfigureAuthServices(IServiceCollection services)
        {
            // identity
            string csTemplate = Configuration.GetConnectionString("Default");

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(string.Format(csTemplate,
                    Configuration.GetValue<string>("DatabaseName")));
            });

            services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            // authentication service
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
               .AddJwtBearer(options =>
               {
                   // NOTE: remember to set the values in configuration:
                   // Jwt:SecureKey, Jwt:Audience, Jwt:Issuer
                   IConfigurationSection jwtSection = Configuration.GetSection("Jwt");
                   string key = jwtSection["SecureKey"];
                   if (string.IsNullOrEmpty(key))
                       throw new ApplicationException("Required JWT SecureKey not found");

                   options.SaveToken = true;
                   options.RequireHttpsMetadata = false;
                   options.TokenValidationParameters = new TokenValidationParameters()
                   {
                       ValidateIssuer = true,
                       ValidateAudience = true,
                       ValidAudience = jwtSection["Audience"],
                       ValidIssuer = jwtSection["Issuer"],
                       IssuerSigningKey = new SymmetricSecurityKey(
                           Encoding.UTF8.GetBytes(key))
                   };
               });
#if DEBUG
            // use to show more information when troubleshooting JWT issues
            IdentityModelEventSource.ShowPII = true;
#endif
        }

        private static void ConfigureSwaggerServices(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "API",
                    Description = "Pleiades Services"
                });
                c.DescribeAllParametersInCamelCase();

                // include XML comments
                // (remember to check the build XML comments in the prj props)
                string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                    c.IncludeXmlComments(xmlPath);

                // JWT
                // https://stackoverflow.com/questions/58179180/jwt-authentication-and-swagger-with-net-core-3-0
                // (cf. https://ppolyzos.com/2017/10/30/add-jwt-bearer-authorization-to-swagger-and-asp-net-core/)
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please insert JWT with Bearer into field",
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
                });
            });
        }

        /// <summary>
        /// Configures the services.
        /// </summary>
        /// <param name="services">The services.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            // configuration
            // ConfigureOptionsServices(services);

            // CORS (before MVC)
            ConfigureCorsServices(services);

            // base services
            services.AddControllers();
            // camel-case JSON in response
            services.AddMvc()
                // https://docs.microsoft.com/en-us/aspnet/core/migration/22-to-30?view=aspnetcore-2.2&tabs=visual-studio#jsonnet-support
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy =
                        JsonNamingPolicy.CamelCase;
                });
                //.SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            // authentication
            ConfigureAuthServices(services);

            // Add framework services
            // for IMemoryCache: https://docs.microsoft.com/en-us/aspnet/core/performance/caching/memory
            services.AddMemoryCache();

            // user repository service
            //services.AddScoped<IUserRepository<ApplicationUser>,
            //    ApplicationUserRepository>();

            // messaging
            //services.AddScoped<IMailerService, DotNetMailerService>();
            //services.AddScoped<IMessageBuilderService,
            //    FileMessageBuilderService>();

            // pleiades
            services.AddScoped<IQuickSearch>((_) =>
            {
                return new PgSqlQuickSearch(
                    string.Format(Configuration.GetConnectionString("Default"),
                    Configuration.GetValue<string>("DatabaseName")));
            });

            // configuration
            services.AddSingleton(_ => Configuration);
            // swagger
            ConfigureSwaggerServices(services);

            // serilog
            // Install-Package Serilog.Exceptions Serilog.Sinks.MongoDB
            // https://github.com/RehanSaeed/Serilog.Exceptions
            string maxSize = Configuration["Serilog:MaxMbSize"];
            services.AddSingleton<ILogger>(
                _ => new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.WithExceptionDetails()
                .WriteTo.Console()
                .CreateLogger());
        }

        /// <summary>
        /// Configures the specified application.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="env">The environment.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-2.2#configure-a-reverse-proxy-server
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor
                    | ForwardedHeaders.XForwardedProto
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-5.0&tabs=visual-studio
                app.UseExceptionHandler("/Error");
                if (Configuration.GetValue<bool>("Server:UseHSTS"))
                {
                    Console.WriteLine("Using HSTS");
                    app.UseHsts();
                }
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            // CORS
            app.UseCors("CorsPolicy");
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Swagger
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                //options.SwaggerEndpoint("/swagger/v1/swagger.json", "V1 Docs");
                string url = Configuration.GetValue<string>("Swagger:Endpoint");
                if (string.IsNullOrEmpty(url)) url = "v1/swagger.json";
                options.SwaggerEndpoint(url, "V1 Docs");
            });
        }
    }
}
