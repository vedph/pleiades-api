using Fusi.DbManager;
using Fusi.DbManager.PgSql;
using Microsoft.Extensions.Configuration;
using Pleiades.Cli.Services;
using Pleiades.Ef;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Pleiades.Cli.Commands;

internal sealed class CreateDbCommand : AsyncCommand<CreateDbCommandSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context,
        CreateDbCommandSettings settings)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\nCREATE DATABASE\n");
        Console.ResetColor();
        Console.WriteLine(
            $"Database name: {settings.DbName}\n");

        // create database if not exists
        string csTemplate = CliAppContext.Configuration.GetConnectionString("Default")!;

        IDbManager manager = new PgSqlDbManager(csTemplate);
        if (manager.Exists(settings.DbName))
        {
            Console.Write($"Database {settings.DbName} already exists");
            return Task.FromResult(0);
        }

        Console.Write($"Creating {settings.DbName}...");
        manager.CreateDatabase(settings.DbName, PleiadesDbSchema.Get(), null);
        Console.WriteLine(" done");

        return Task.FromResult(0);
    }
}

internal class CreateDbCommandSettings: CommandSettings
{
    [CommandOption("-d|--db <NAME>")]
    [DefaultValue("pleiades")]
    public string DbName { get; set; }

    public CreateDbCommandSettings()
    {
        DbName = "pleiades";
    }
}
