using Fusi.DbManager;
using Fusi.DbManager.PgSql;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pleiades.Tool.Commands
{
    public sealed class BulkExportCommand : ICommand
    {
        private readonly IConfiguration _config;
        private readonly string _dbName;
        private readonly string _targetDir;

        public BulkExportCommand(AppOptions options, string dbName, string targetDir)
        {
            _config = options.Configuration;
            _dbName = dbName ?? throw new ArgumentNullException(nameof(dbName));
            _targetDir = targetDir ?? throw new ArgumentNullException(nameof(targetDir));
        }

        public static void Configure(CommandLineApplication command,
            AppOptions options)
        {
            command.Description = "Bulk export the Pleiades database table data " +
                "into a set of BLOB files.";
            command.HelpOption("-?|-h|--help");

            CommandArgument dbNameArgument = command.Argument("[dbName]",
                "The database name.");

            CommandArgument targetDirArgument = command.Argument("[targetDir]",
                "The target directory.");

            command.OnExecute(() =>
            {
                options.Command = new BulkExportCommand(
                    options,
                    dbNameArgument.Value,
                    targetDirArgument.Value);
                return 0;
            });
        }

        public Task Run()
        {
            string connection = string.Format(_config.GetConnectionString("Default"),
                _dbName);
            IBulkTableCopier tableCopier = new PgSqlBulkTableCopier(connection);
            BulkTablesCopier copier = new(tableCopier);
            copier.Write(new[]
            {
                "lookup", "eix_token", "eix_occurrence",
                "author", "place", "place_author_link",
                "place_attestation", "place_feature", "place_link", "place_meta",
                "place_reference", "name", "name_attestation", "name_author_link",
                "name_reference", "location", "location_attestation",
                "location_author_link", "location_meta", "location_reference",
                "connection", "connection_attestation", "connection_author_link",
                "connection_reference"
            }, _targetDir, CancellationToken.None,
            new Progress<string>((s) => Console.WriteLine(s)));

            return Task.FromResult(0);
        }
    }
}
