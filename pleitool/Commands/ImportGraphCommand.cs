using Fusi.DbManager;
using Fusi.DbManager.PgSql;
using Fusi.Tools;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pleiades.Ef;
using Pleiades.Ef.PgSql;
using Pleiades.Migration;
using Pleiades.Tool.Services;
using ShellProgressBar;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pleiades.Tool.Commands
{
    public sealed class ImportGraphCommand : ICommand
    {
        private readonly IConfiguration _config;
        private readonly string _inputFile;
        private readonly string _dbName;
        private readonly bool _preflight;
        private readonly int _skip;
        private readonly int _limit;
        private readonly PlaceChildFlags _flags;
        private readonly string _connection;

        public ILogger Logger { get; set; }

        public ImportGraphCommand(AppOptions options, string inputFile,
            string dbName, bool preflight, string flags, int skip, int limit)
        {
            _config = options.Configuration;
            _inputFile = inputFile;
            _dbName = dbName ?? "pleiades";
            _preflight = preflight;
            _skip = skip;
            _limit = limit;
            _flags = flags != null? ParseFlags(flags) : PlaceChildFlags.All;
            _connection = string.Format(CultureInfo.InvariantCulture,
                _config.GetConnectionString("Default"),
                _dbName);
        }

        private static PlaceChildFlags ParseFlags(string text)
        {
            PlaceChildFlags flags = 0;

            foreach (char c in text.ToUpperInvariant())
            {
                switch (c)
                {
                    case 'F':
                        flags |= PlaceChildFlags.Features;
                        break;
                    case 'C':
                        flags |= PlaceChildFlags.Connections;
                        break;
                    case 'O':
                        flags |= PlaceChildFlags.Contributors;
                        break;
                    case 'L':
                        flags |= PlaceChildFlags.Locations;
                        break;
                    case 'E':
                        flags |= PlaceChildFlags.Connections;
                        break;
                    case 'A':
                        flags |= PlaceChildFlags.Attestations;
                        break;
                    case 'R':
                        flags |= PlaceChildFlags.References;
                        break;
                    case 'N':
                        flags |= PlaceChildFlags.Names;
                        break;
                    case 'M':
                        flags |= PlaceChildFlags.Metadata;
                        break;
                    case 'T':
                        flags |= PlaceChildFlags.TargetUris;
                        break;
                    case '0':
                        flags = 0;
                        break;
                }
            }
            return flags;
        }

        public static void Configure(CommandLineApplication command,
            AppOptions options)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            command.Description = "Import Pleiades dataset from JSON file";
            command.HelpOption("-?|-h|--help");

            CommandArgument inputArgument = command.Argument("[input-path]",
                "The input JSON file path");

            CommandOption dbNameOption = command.Option("-d|--database",
                "Database name",
                CommandOptionType.SingleValue);

            CommandOption preflightOption = command.Option("-p|--preflight",
                "Preflight mode -- dont' write data to DB",
                CommandOptionType.NoValue);

            CommandOption skipOption = command.Option("-s|--skip",
                "Skip the first N places in import",
                CommandOptionType.SingleValue);

            CommandOption limitOption = command.Option("-l|--limit",
                "Limit import to the first N places",
                CommandOptionType.SingleValue);

            CommandOption flagsOption = command.Option("-f|--flags",
                "Import only the specified place children: " +
                "[F]eatures [C]reators c[O]ntributors [L]ocations " +
                "conn[E]ctions [A]ttestations [R]eferences [N]ames " +
                "[M]etadata [T]argetURIs [0]=none",
                CommandOptionType.SingleValue);

            command.OnExecute(() =>
            {
                int skip = 0;
                if (skipOption.HasValue()
                    && int.TryParse(skipOption.Value(), out int ns))
                {
                    skip = ns;
                }

                int limit = 0;
                if (limitOption.HasValue()
                    && int.TryParse(limitOption.Value(), out int nl))
                {
                    limit = nl;
                }

                options.Command = new ImportGraphCommand(
                    options,
                    inputArgument.Value,
                    dbNameOption.Value(),
                    preflightOption.HasValue(),
                    flagsOption.HasValue()? flagsOption.Value() : null,
                    skip,
                    limit);
                return 0;
            });
        }

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nIMPORT JSON DATASET\n");
            Console.ResetColor();
            Console.WriteLine(
                $"Input file: {_inputFile}\n" +
                $"Database name: {_dbName}\n" +
                $"Preflight: {_preflight}\n" +
                $"Flags: {_flags}");
            if (_skip > 0) Console.WriteLine("Skip: " + _skip);
            if (_limit > 0) Console.WriteLine("Limit: " + _limit);
            Console.WriteLine();

            // create database if not exists
            if (!_preflight)
            {
                string connection = _config.GetConnectionString("Default");
                IDbManager manager = new PgSqlDbManager(connection);
                if (manager.Exists(_dbName))
                {
                    Console.Write($"Clearing {_dbName}...");
                    manager.ClearDatabase(_dbName);
                    Console.WriteLine(" done");
                }
                else
                {
                    Console.Write($"Creating {_dbName}...");
                    manager.CreateDatabase(_dbName,
                        PleiadesDbSchema.Get(), null);
                    Console.WriteLine(" done");
                }
            }

            using (ProgressBar progressBar = new(100,
                $"Importing from {_inputFile}...",
                new ProgressBarOptions
                {
                    EnableTaskBarProgress = true
                }))
            using (Stream stream = new FileStream(_inputFile, FileMode.Open,
                FileAccess.Read, FileShare.Read))
            {
                JsonPlaceReader reader = new(stream, null)
                {
                    Logger = Logger
                };
                EfPlaceAdapter adapter = new(reader.LookupSet);

                IPleiadesContextFactory contextFactory =
                    new PgSqlPleiadesContextFactory(_connection);

                using EfPleiadesWriter writer = new(contextFactory)
                {
                    Logger = Logger
                };

                PlaceImporter importer = new(
                    reader,
                    writer,
                    adapter)
                {
                    Logger = Logger,
                    IsPreflight = _preflight,
                    Skip = _skip,
                    Limit = _limit,
                    ImportFlags = _flags
                };
                int count = importer.Import(CancellationToken.None,
                    new Progress<ProgressReport>(
                        r => progressBar.Tick(r.Percent, r.Message)));

                Console.WriteLine("\n\n\nPlaces imported: " + count);
                Console.WriteLine("Places read: " + reader.Position);
            }

            return Task.CompletedTask;
        }
    }
}
