using Fusi.DbManager;
using Fusi.DbManager.PgSql;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Pleiades.Tool.Commands
{
    public sealed class PopulateSpatialCommand : ICommand
    {
        private readonly IConfiguration _config;
        private readonly string _dbName;

        public ILogger Logger { get; set; }

        public PopulateSpatialCommand(AppOptions options, string dbName)
        {
            _config = options.Configuration;
            _dbName = dbName ?? "pleiades";
        }

        public static void Configure(CommandLineApplication command,
            AppOptions options)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            command.Description = "Populate spatial data in an imported database.";
            command.HelpOption("-?|-h|--help");

            CommandArgument dbArgument = command.Argument("[database]",
                "The name of the database");

            command.OnExecute(() =>
            {
                options.Command = new PopulateSpatialCommand(
                    options,
                    dbArgument.Value);
                return 0;
            });
        }

        private static string LoadResourceText(string name)
        {
            using StreamReader reader = new StreamReader(
                Assembly.GetExecutingAssembly().GetManifestResourceStream(
                    $"Pleiades.Tool.Assets.{name}"), Encoding.UTF8);
            return reader.ReadToEnd();
        }

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nPOPULATE SPATIAL\n");
            Console.ResetColor();
            Console.WriteLine($"Database name: {_dbName}\n");

            string connection = _config.GetConnectionString("Default");

            IDbManager manager = new PgSqlDbManager(connection);
            string sql = LoadResourceText("Spatial.pgsql");
            manager.ExecuteCommands(_dbName, sql.Split(';'));

            return Task.CompletedTask;
        }
    }
}
