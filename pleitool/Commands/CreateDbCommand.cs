using Fusi.DbManager;
using Fusi.DbManager.PgSql;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pleiades.Ef;
using Pleiades.Tool.Services;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Pleiades.Tool.Commands
{
    public sealed class CreateDbCommand : ICommand
    {
        private readonly IConfiguration _config;
        private readonly string _dbName;

        public ILogger Logger { get; set; }

        public CreateDbCommand(AppOptions options, string dbName)
        {
            _config = options.Configuration;
            _dbName = dbName ?? "pleiades";
        }

        public static void Configure(CommandLineApplication command,
            AppOptions options)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            command.Description = "Create an empty Pleiades database with its schema";
            command.HelpOption("-?|-h|--help");

            CommandArgument dbArgument = command.Argument("[database]",
                "The name of the database to create");

            command.OnExecute(() =>
            {
                options.Command = new CreateDbCommand(
                    options,
                    dbArgument.Value);
                return 0;
            });
        }

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nCREATE DATABASE\n");
            Console.ResetColor();
            Console.WriteLine(
                $"Database name: {_dbName}\n");

            // create database if not exists
            string connection = _config.GetConnectionString("Default");

            IDbManager manager = new PgSqlDbManager(connection);
            if (manager.Exists(_dbName))
            {
                Console.Write($"Database {_dbName} already exists");
                return Task.CompletedTask;
            }

            Console.Write($"Creating {_dbName}...");
            manager.CreateDatabase(_dbName, PleiadesDbSchema.Get(), null);
            Console.WriteLine(" done");

            return Task.CompletedTask;
        }
    }
}
