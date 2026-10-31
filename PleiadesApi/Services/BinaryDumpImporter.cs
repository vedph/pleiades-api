using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace PleiadesApi.Services;

/// <summary>
/// A binary table dump file.
/// </summary>
/// <param name="Path">The file path.</param>
/// <param name="Table">The target table name.</param>
/// <param name="Length">The file length in bytes.</param>
public sealed record BinaryDumpFile(string Path, string Table, long Length);

/// <summary>
/// Importer of PostgreSQL binary table dumps, i.e. one file per table named
/// <c>NNNNN_table.bin</c> with the output of <c>COPY table TO STDOUT
/// (FORMAT BINARY)</c>, as written by <c>BulkTablesCopier</c> (Fusi.DbManager)
/// or <c>pleitool export</c>. All the tables are imported in a single
/// transaction, so that a failed import leaves the database unchanged and
/// can be retried.
/// </summary>
public sealed partial class BinaryDumpImporter
{
    private const double MB = 1024 * 1024;

    // tables owned by extensions, which populate them on creation
    private static readonly HashSet<string> _extensionTables =
        new(StringComparer.OrdinalIgnoreCase) { "spatial_ref_sys" };

    private readonly string _connectionString;
    private readonly ILogger _logger;

    [GeneratedRegex(@"^(?<n>[0-9]{5})_(?<t>.+)\.bin$")]
    private static partial Regex DumpFileRegex();

    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryDumpImporter"/>
    /// class.
    /// </summary>
    /// <param name="connectionString">The target database connection
    /// string. As importing may take long, its command timeout should be
    /// disabled.</param>
    /// <param name="logger">The logger.</param>
    public BinaryDumpImporter(string connectionString, ILogger logger)
    {
        _connectionString = connectionString
            ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the dump files in the specified directory, sorted by their
    /// ordinal number.
    /// </summary>
    /// <param name="directory">The directory.</param>
    /// <returns>Files.</returns>
    public static IList<BinaryDumpFile> GetFiles(string directory)
    {
        ArgumentNullException.ThrowIfNull(directory);

        return [.. from path in Directory.EnumerateFiles(directory, "*.bin")
                let m = DumpFileRegex().Match(Path.GetFileName(path))
                where m.Success
                orderby m.Groups["n"].Value, path
                select new BinaryDumpFile(path, m.Groups["t"].Value,
                    new FileInfo(path).Length)];
    }

    /// <summary>
    /// Quotes the specified table name, optionally qualified by its schema.
    /// </summary>
    /// <param name="table">The table name.</param>
    /// <returns>Quoted name.</returns>
    public static string QuoteTable(string table) =>
        string.Join('.', table.Split('.').Select(
            p => "\"" + p.Trim().Trim('"').Replace("\"", "\"\"") + "\""));

    private static object? ExecuteScalar(NpgsqlConnection connection,
        string sql, params object[] args)
    {
        using NpgsqlCommand cmd = new(sql, connection);
        foreach (object arg in args) cmd.Parameters.Add(new() { Value = arg });
        return cmd.ExecuteScalar();
    }

    private static void ExecuteNonQuery(NpgsqlConnection connection,
        string sql)
    {
        using NpgsqlCommand cmd = new(sql, connection);
        cmd.ExecuteNonQuery();
    }

    private static string FormatSize(long bytes) =>
        $"{bytes / MB:N1} MB";

    private void ImportTable(NpgsqlConnection connection, BinaryDumpFile file,
        string table, int n, int count, CancellationToken cancel)
    {
        _logger.LogInformation("Importing table {N}/{Count} {Table} " +
            "from {File} ({Size})...", n, count, file.Table,
            Path.GetFileName(file.Path), FormatSize(file.Length));
        Stopwatch watch = Stopwatch.StartNew();

        using (FileStream input = new(file.Path, FileMode.Open,
            FileAccess.Read, FileShare.Read))
        using (NpgsqlRawCopyStream output = connection.BeginRawBinaryCopy(
            $"COPY {table} FROM STDIN (FORMAT BINARY)"))
        {
            byte[] buffer = new byte[81920];
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (cancel.IsCancellationRequested)
                {
                    output.Cancel();
                    cancel.ThrowIfCancellationRequested();
                }
                output.Write(buffer, 0, len);
            }
        }

        long rows = Convert.ToInt64(ExecuteScalar(connection,
            $"SELECT COUNT(*) FROM {table}"));
        _logger.LogInformation("Table {Table} imported: {Rows} rows in {Elapsed}",
            file.Table, rows, watch.Elapsed);
    }

    /// <summary>
    /// Imports the specified files. The target tables must exist; those which
    /// are not empty are cleared before importing, while tables owned by
    /// extensions (like PostGIS <c>spatial_ref_sys</c>) are skipped. After
    /// importing, identity and serial sequences are synchronized with the
    /// imported IDs.
    /// </summary>
    /// <param name="files">The files.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <exception cref="InvalidOperationException">Missing tables.</exception>
    public void Import(IList<BinaryDumpFile> files, CancellationToken cancel)
    {
        ArgumentNullException.ThrowIfNull(files);

        List<BinaryDumpFile> targets = [];
        foreach (BinaryDumpFile file in files)
        {
            if (_extensionTables.Contains(file.Table.Split('.')[^1].Trim('"')))
            {
                _logger.LogInformation("Skipping {File}: table {Table} is " +
                    "populated by its extension",
                    Path.GetFileName(file.Path), file.Table);
            }
            else
            {
                targets.Add(file);
            }
        }

        using NpgsqlConnection connection = new(_connectionString);
        connection.Open();
        using NpgsqlTransaction tx = connection.BeginTransaction();

        // replica mode disables triggers, including FK checks, so that
        // tables can be imported in any order; this requires a superuser
        if (ExecuteScalar(connection, "SHOW is_superuser") as string == "on")
        {
            ExecuteNonQuery(connection,
                "SET LOCAL session_replication_role = replica");
        }
        else
        {
            _logger.LogWarning("User is not a superuser: foreign keys will " +
                "be checked while importing, so tables must be in dependency " +
                "order");
        }

        // all the target tables must exist
        List<string> missing = [.. targets
            .Where(f => ExecuteScalar(connection,
                "SELECT to_regclass($1) IS NOT NULL", QuoteTable(f.Table))
                is not true)
            .Select(f => f.Table)];
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Dump tables not found in database: " +
                string.Join(", ", missing));
        }

        // clear the target tables which are not empty (e.g. accounts)
        foreach (BinaryDumpFile file in targets)
        {
            string table = QuoteTable(file.Table);
            if (ExecuteScalar(connection,
                $"SELECT EXISTS (SELECT 1 FROM {table})") is true)
            {
                _logger.LogWarning("Clearing non-empty table {Table} " +
                    "before importing it", file.Table);
                ExecuteNonQuery(connection, $"DELETE FROM {table}");
            }
        }

        // import
        Stopwatch watch = Stopwatch.StartNew();
        long total = targets.Sum(f => f.Length);
        _logger.LogInformation("Importing {Count} tables ({Size})...",
            targets.Count, FormatSize(total));
        for (int i = 0; i < targets.Count; i++)
        {
            ImportTable(connection, targets[i], QuoteTable(targets[i].Table),
                i + 1, targets.Count, cancel);
        }

        // sync sequences with the imported IDs
        _logger.LogInformation("Synchronizing sequences...");
        ExecuteNonQuery(connection, """
            DO $$
            DECLARE r record;
            BEGIN
              FOR r IN
                SELECT table_schema, table_name, column_name
                FROM information_schema.columns
                WHERE table_schema = current_schema()
                AND pg_get_serial_sequence(
                  quote_ident(table_schema) || '.' || quote_ident(table_name),
                  column_name) IS NOT NULL
              LOOP
                EXECUTE format(
                  'SELECT setval(pg_get_serial_sequence(%L, %L), ' ||
                  'COALESCE(MAX(%I), 1), MAX(%I) IS NOT NULL) FROM %I.%I',
                  quote_ident(r.table_schema) || '.' || quote_ident(r.table_name),
                  r.column_name, r.column_name, r.column_name,
                  r.table_schema, r.table_name);
              END LOOP;
            END $$;
            """);

        tx.Commit();
        _logger.LogInformation("Imported {Count} tables ({Size}) in {Elapsed}",
            targets.Count, FormatSize(total), watch.Elapsed);
    }
}
