using Fusi.Tools;
using Microsoft.Extensions.Logging;
using Pleiades.Core;
using Pleiades.Ef;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Pleiades.Migration
{
    /// <summary>
    /// Places importer. This uses a <see cref="JsonPlaceReader"/> to read
    /// places from the Pleiades JSON export, an <see cref="EfPleiadesWriter"/>
    /// to write data into a database, and an <see cref="EfPlaceAdapter"/>
    /// to adapt place's nodes into their EF-based counterparts.
    /// </summary>
    public sealed class PlaceImporter
    {
        private readonly JsonPlaceReader _reader;
        private readonly EfPleiadesWriter _writer;
        private readonly List<PendingLink> _pendingLinks;
        private readonly HashSet<string> _pendingLinkNames;
        private readonly EfPlaceAdapter _adapter;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this importer should
        /// work in preflight mode. In this mode, no data is written to the
        /// database.
        /// </summary>
        public bool IsPreflight { get; set; }

        /// <summary>
        /// Gets or sets the count of initial places to skip in import.
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// Gets or sets the limit of places to be imported (0=import all).
        /// </summary>
        public int Limit { get; set; }

        /// <summary>
        /// Gets or sets the import flags, defining the child nodes of
        /// place nodes to be imported. The default value is
        /// <see cref="PlaceChildFlags.All"/>.
        /// </summary>
        public PlaceChildFlags ImportFlags { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaceImporter"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="writer">The writer.</param>
        /// <param name="adapter">The adapter.</param>
        /// <exception cref="ArgumentNullException">reader or db</exception>
        public PlaceImporter(JsonPlaceReader reader, EfPleiadesWriter writer,
            EfPlaceAdapter adapter)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));

            // all the place-to-place connections or links whose target
            // has not already been stored in the DB are cached as pending,
            // and processed later.
            _pendingLinks = new List<PendingLink>();
            _pendingLinkNames = new HashSet<string>();

            ImportFlags = PlaceChildFlags.All;
        }

        #region Pending
        private void ClearPending()
        {
            _pendingLinks.Clear();
            _pendingLinkNames.Clear();
        }

        private void AddPending(PendingLink link)
        {
            _pendingLinks.Add(link);
            _pendingLinkNames.Add(link.OriginalId);
        }

        private void ResolvePendingFrom(Place place)
        {
            if (!_pendingLinkNames.Contains(place.Uri)) return;

            foreach (var pending in _pendingLinks
                .Where(p => p.OriginalId == place.Uri))
            {
                pending.ResolvedId = place.Id;
            }
        }
        #endregion

        /// <summary>
        /// Imports data into the database.
        /// </summary>
        /// <param name="cancel">The cancellation token.</param>
        /// <param name="progress">The optional progress reporter.</param>
        /// <returns>Count of places imported.</returns>
        public int Import(CancellationToken cancel,
            IProgress<ProgressReport> progress = null)
        {
            ProgressReport report = progress != null ?
                new ProgressReport() : null;
            ClearPending();

            Place place;
            int placesToSkip = Skip;
            int placeCount = 0;

            while ((place = _reader.Read()) != null)
            {
                if (placesToSkip > 0)
                {
                    placesToSkip--;
                    if (progress != null)
                    {
                        report.Message = "SKIP " + place;
                        report.Percent = _reader.Position * 100 / _reader.Length;
                        progress.Report(report);
                    }
                    continue;
                }
                if (Limit > 0 && placeCount + 1 > Limit) break;
                placeCount++;

                // progress
                if (progress != null)
                {
                    report.Percent = _reader.Position * 100 / _reader.Length;
                    report.Message = $"{_reader.Position:000000} {place}";
                    progress.Report(report);
                }

                var t = _adapter.GetPlace(place, ImportFlags);
                foreach (var link in t.Item2) AddPending(link);
                ResolvePendingFrom(place);

                if (!IsPreflight) _writer.WritePlace(t.Item1);

                // cancellation
                if (cancel.IsCancellationRequested) break;
            }

            // lookup, connections and links
            if (!IsPreflight)
            {
                _writer.Flush();
                _writer.WriteLookups(_reader.LookupSet.GetLookups()
                    .Select(l => new EfLookup
                {
                    Id = l.Id,
                    FullName = l.FullName,
                    ShortName = l.ShortName,
                    Group = l.Group
                }).ToList());
                _writer.ResolvePending(_pendingLinks);
            }

            return placeCount;
        }
    }
}
