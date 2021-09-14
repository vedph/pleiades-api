using BAMCIS.GeoJSON;
using Fusi.Tools;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Data;
using System.Threading;

namespace Pleiades.Geo
{
    /// <summary>
    /// Geometries validator for PostgreSql.
    /// </summary>
    public sealed class GeoValidator
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;

        public GeoValidator(string connectionString, ILogger logger)
        {
            _connectionString = connectionString
                ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private int ValidatePlace(IDbConnection connection,
            CancellationToken cancel,
            IProgress<ProgressReport> progress = null)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM place;";
            int count = 0, errCount = 0, total = Convert.ToInt32(cmd.ExecuteScalar());
            if (total == 0) return 0;

            ProgressReport report = progress != null ?
                new ProgressReport() : null;

            cmd.CommandText = "SELECT id,rp_lat,rp_lon," +
                "bbox_sw_lat,bbox_sw_lon,bbox_ne_lat,bbox_ne_lon " +
                "FROM place;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                count++;
                string id = reader.GetString(0);

                // rplat
                double d = reader.GetDouble(1);
                if (d < -90 || d > 90)
                {
                    errCount++;
                    _logger.LogError($"Invalid lat {d} at place #{id}");
                }
                // rplon
                d = reader.GetDouble(2);
                if (d < -180 || d > 180)
                {
                    errCount++;
                    _logger.LogError($"Invalid lon {d} at place #{id}");
                }
                // bboxswlat
                d = reader.GetDouble(3);
                if (d < -90 || d > 90)
                {
                    errCount++;
                    _logger.LogError($"Invalid bbox_sw_lat {d} at place #{id}");
                }
                // bboxswlon
                d = reader.GetDouble(4);
                if (d < -180 || d > 180)
                {
                    errCount++;
                    _logger.LogError($"Invalid bbox_sw_lon {d} at place #{id}");
                }
                // bboxnelat
                d = reader.GetDouble(5);
                if (d < -90 || d > 90)
                {
                    errCount++;
                    _logger.LogError($"Invalid bbox_ne_lat {d} at place #{id}");
                }
                // bboxnelon
                d = reader.GetDouble(6);
                if (d < -180 || d > 180)
                {
                    errCount++;
                    _logger.LogError($"Invalid bbox_ne_lon {d} at place #{id}");
                }

                if (progress != null && count % 10 == 0)
                {
                    report.Percent = count * 100 / total;
                    progress.Report(report);
                }
                if (cancel.IsCancellationRequested) break;
            }
            reader.Close();
            return errCount;
        }

        private int ValidateGeometries(IDataReader reader, int total,
            string table,
            CancellationToken cancel,
            IProgress<ProgressReport> progress)
        {
            ProgressReport report = progress != null ?
                new ProgressReport() : null;

            int count = 0, errCount = 0;

            while (reader.Read())
            {
                count++;
                int id = reader.GetInt32(0);
                string json = reader.GetString(1);
                try
                {
                    GeoJson gj = JsonConvert.DeserializeObject<GeoJson>(json);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Invalid GeoJSON in {table} at #{id}: " +
                        $"{ex.Message}: \"{json}\"");
                    errCount++;
                }

                if (cancel.IsCancellationRequested) break;
                if (progress != null && count % 10 == 0)
                {
                    report.Percent = count * 100 / total;
                    progress.Report(report);
                }
            }
            return errCount;
        }

        public int Validate(CancellationToken cancel,
            IProgress<ProgressReport> progress = null)
        {
            using IDbConnection connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            int errCount = 0;

            // place
            errCount += ValidatePlace(connection, cancel, progress);

            // place_feature.geometry
            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM place_feature " +
                "WHERE \"geometry\" IS NOT NULL;";
            int total = Convert.ToInt32(cmd.ExecuteScalar());

            cmd.CommandText = "SELECT id,\"geometry\" FROM place_feature " +
                "WHERE \"geometry\" IS NOT NULL;";
            using (IDataReader reader = cmd.ExecuteReader())
            {
                errCount += ValidateGeometries(reader, total, "place_feature",
                    cancel, progress);
            }

            // location.geometry
            cmd.CommandText = "SELECT COUNT(*) FROM location " +
                "WHERE \"geometry\" IS NOT NULL;";
            total = Convert.ToInt32(cmd.ExecuteScalar());
            cmd.CommandText = "SELECT id,\"geometry\" FROM location " +
                "WHERE \"geometry\" IS NOT NULL;";
            using (IDataReader reader = cmd.ExecuteReader())
            {
                errCount += ValidateGeometries(reader, total, "location",
                    cancel, progress);
            }

            if (progress != null && !cancel.IsCancellationRequested)
                progress.Report(new ProgressReport { Percent = 100 });

            return errCount;
        }
    }
}
