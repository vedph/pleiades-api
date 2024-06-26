﻿using Fusi.DbManager;
using Fusi.DbManager.PgSql;
using Microsoft.Extensions.Configuration;
using Pleiades.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pleiades.Cli.Commands;

/// <summary>
/// Bulk export the Pleiades database table data into a set of BLOB files.
/// </summary>
internal sealed class BulkExportCommand : AsyncCommand<BulkExportCommandSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context,
        BulkExportCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[green underline]BULK DATA EXPORT[/]");
        AnsiConsole.MarkupLine($"Database: [cyan]{settings.DbName}[/]");
        AnsiConsole.MarkupLine($"Target dir: [cyan]{settings.TargetDir}[/]");

        if (!Directory.Exists(settings.TargetDir))
            Directory.CreateDirectory(settings.TargetDir);

        string cs = string.Format(CliAppContext.Configuration.
            GetConnectionString("Default")!, settings.DbName);

        IBulkTableCopier tableCopier = new PgSqlBulkTableCopier(cs);
        BulkTablesCopier copier = new(tableCopier);
        copier.Write(
        [
            "lookup", "eix_token", "eix_occurrence",
            "author", "place", "place_author_link",
            "place_attestation", "place_feature", "place_link", "place_meta",
            "place_reference", "name", "name_attestation", "name_author_link",
            "name_reference", "location", "location_attestation",
            "location_author_link", "location_meta", "location_reference",
            "connection", "connection_attestation", "connection_author_link",
            "connection_reference"
        ], settings.TargetDir, CancellationToken.None,
        new Progress<string>(Console.WriteLine));

        return Task.FromResult(0);
    }
}

internal class BulkExportCommandSettings : CommandSettings
{
    [CommandArgument(0, "<TARGET_DIR>")]
    public string TargetDir { get; set; }

    [CommandOption("-d|--db <NAME>")]
    [DefaultValue("pleiades")]
    public string DbName { get; set; }

    public BulkExportCommandSettings()
    {
        TargetDir = "";
        DbName = "pleiades";
    }
}
