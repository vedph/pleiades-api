using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Pleiades.Ef;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pleiades.Migration;

/// <summary>
/// EF-based Pleiades data writer. This writer uses a
/// <see cref="PleiadesDbContext"/> (built via a received
/// <see cref="IPleiadesContextFactory"/>) to write places into a database,
/// by repeatedly filling and flushing an internal cache to maximize
/// performance. The context change tracker is cleared after each flush,
/// so that its size (and the cost of detecting changes) does not grow
/// with the count of imported places.
/// </summary>
public sealed class EfPleiadesWriter : IDisposable
{
    private readonly List<EfPlace> _places;
    private readonly PleiadesDbContext _context;
    // authors are the only entities shared across places: once saved,
    // they must not be inserted again by later places referring to them
    private readonly HashSet<string> _savedAuthorIds;
    private bool _disposedValue;

    /// <summary>
    /// Gets or sets the size of the cache.
    /// </summary>
    public int CacheSize { get; set; }

    /// <summary>
    /// Gets or sets the logger.
    /// </summary>
    public ILogger? Logger { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EfPleiadesWriter"/> class.
    /// </summary>
    /// <param name="factory">The Pleiades DB context factory.</param>
    /// <exception cref="ArgumentNullException">factory</exception>
    public EfPleiadesWriter(IPleiadesContextFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        _places = [];
        _savedAuthorIds = [];
        _context = factory.GetContext();
        CacheSize = 50;
    }

    private void SaveAndClear()
    {
        // authors already saved are tracked as added when reached from
        // the newly added graphs: mark them as unchanged
        foreach (EntityEntry<EfAuthor> entry in _context.ChangeTracker
            .Entries<EfAuthor>()
            .Where(e => e.State == EntityState.Added
                && _savedAuthorIds.Contains(e.Entity.Id!)))
        {
            entry.State = EntityState.Unchanged;
        }

        _context.SaveChanges();

        foreach (EntityEntry<EfAuthor> entry in _context.ChangeTracker
            .Entries<EfAuthor>())
        {
            _savedAuthorIds.Add(entry.Entity.Id!);
        }
        _context.ChangeTracker.Clear();
    }

    /// <summary>
    /// Writes the specified place into the database.
    /// </summary>
    /// <param name="place">The place.</param>
    /// <exception cref="ArgumentNullException">place</exception>
    public void WritePlace(EfPlace place)
    {
        ArgumentNullException.ThrowIfNull(place);

        _places.Add(place);
        if (_places.Count >= CacheSize) Flush();
    }

    /// <summary>
    /// Writes the lookup data to the database.
    /// </summary>
    /// <param name="lookups">The lookups.</param>
    /// <exception cref="ArgumentNullException">lookups</exception>
    public void WriteLookups(IList<EfLookup> lookups)
    {
        ArgumentNullException.ThrowIfNull(lookups);

        _context.Lookups.AddRange(lookups);
        SaveAndClear();
    }

    /// <summary>
    /// Resolves the pending connections and place links, writing them
    /// into the database. Unresolved links are logged and skipped, and
    /// duplicate place links are written once.
    /// </summary>
    /// <param name="links">The links.</param>
    /// <exception cref="ArgumentNullException">links</exception>
    public void ResolvePending(IList<PendingLink> links)
    {
        ArgumentNullException.ThrowIfNull(links);

        List<EfConnection> resConnections = [];
        List<EfPlaceLink> resLinks = [];
        HashSet<(string, string)> linkKeys = [];

        foreach (PendingLink link in links)
        {
            if (link.ResolvedId == null)
            {
                Logger?.LogError("Unresolved link ID: {Link} ({Target})",
                    link, link.OriginalId);
                continue;
            }

            switch (link.Payload)
            {
                case EfConnection efc:
                    efc.TargetId = link.ResolvedId;
                    resConnections.Add(efc);
                    break;
                case EfPlaceLink efl:
                    efl.TargetId = link.ResolvedId;
                    if (linkKeys.Add((efl.SourceId!, efl.TargetId)))
                        resLinks.Add(efl);
                    break;
            }
        }

        _context.Connections.AddRange(resConnections);
        SaveAndClear();

        _context.PlaceLinks.AddRange(resLinks);
        SaveAndClear();
    }

    /// <summary>
    /// Flushes this writer.
    /// </summary>
    public void Flush()
    {
        if (_places.Count == 0) return;

        _context.Places.AddRange(_places);
        SaveAndClear();
        _places.Clear();
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                Flush();
                _context.Dispose();
            }
            _disposedValue = true;
        }
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing,
    /// releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
