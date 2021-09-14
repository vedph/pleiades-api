using Fusi.Tools;
using Microsoft.Extensions.CommandLineUtils;
using Pleiades.Geo;
using ShellProgressBar;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pleiades.Tool.Commands
{
    public sealed class ValidateGeoCommand : ICommand
    {
        private readonly AppOptions _options;
        private readonly string _dbName;

        public ValidateGeoCommand(AppOptions options, string dbName)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _dbName = dbName ?? throw new ArgumentNullException(nameof(dbName));
        }

        public static void Configure(CommandLineApplication command,
            AppOptions options)
        {
            command.Description = "Validate geometries in Pleiades database.";
            command.HelpOption("-?|-h|--help");

            CommandArgument dbNameArgument = command.Argument("[dbName]",
                "The database name");

            command.OnExecute(() =>
            {
                options.Command = new ValidateGeoCommand(
                    options,
                    dbNameArgument.Value);
                return 0;
            });
        }

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("VALIDATE GEO\n");
            Console.ResetColor();

            GeoValidator validator = new GeoValidator(
                string.Format(
                    _options.Configuration["ConnectionStrings:PgSql"], _dbName),
                _options.Logger);

            ProgressBar bar = new ProgressBar(100, null, new ProgressBarOptions
            {
                DisplayTimeInRealTime = false,
                EnableTaskBarProgress = true,
                CollapseWhenFinished = true
            });
            int errors = validator.Validate(CancellationToken.None,
                new Progress<ProgressReport>(
                    report => bar.Tick(report.Percent)));

            if (errors > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("Errors: " + errors);
                Console.ResetColor();
                Console.WriteLine();
            }
            else
            {
                Console.Write("No errors.");
            }

            return Task.CompletedTask;
        }
    }
}
