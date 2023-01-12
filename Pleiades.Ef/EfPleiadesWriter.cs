using Microsoft.Extensions.Logging;
using Pleiades.Ef;
using System;
using System.Collections.Generic;

namespace Pleiades.Migration;

/// <summary>
/// EF-based Pleiades data writer. This writer uses a
/// <see cref="PleiadesDbContext"/> (built via a received
/// <see cref="IPleiadesContextFactory"/>) to write places into a database,
/// by repeatedly filling and flushing an internal cache to maximize
/// performance.
/// </summary>
public sealed class EfPleiadesWriter : IDisposable
{
    private readonly List<EfPlace> _places;
    private readonly PleiadesDbContext _context;
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
    /// <exception cref="ArgumentNullException">context</exception>
    public EfPleiadesWriter(IPleiadesContextFactory factory)
    {
        _places = new List<EfPlace>();
        // TODO: get new context at each flush
        _context = factory.GetContext();
        CacheSize = 50;
    }

    /// <summary>
    /// Writes the specified place into the database.
    /// </summary>
    /// <param name="place">The place.</param>
    /// <exception cref="ArgumentNullException">place</exception>
    public void WritePlace(EfPlace place)
    {
        if (place == null)
            throw new ArgumentNullException(nameof(place));

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
        if (lookups == null)
            throw new ArgumentNullException(nameof(lookups));

        _context.Lookups.AddRange(lookups);
        _context.SaveChanges();
    }

    /// <summary>
    /// Resolves the pending connections and place links, writing them
    /// into the database.
    /// </summary>
    /// <param name="links">The links.</param>
    /// <exception cref="ArgumentNullException">links</exception>
    public void ResolvePending(IList<PendingLink> links)
    {
        if (links == null)
            throw new ArgumentNullException(nameof(links));

        List<EfConnection> resConnections = new();
        List<EfPlaceLink> resLinks = new();

        foreach (PendingLink link in links)
        {
            if (link.ResolvedId == null)
            {
                Logger?.LogError($"Unresolved link ID: {link}");
                continue;
            }

            EfConnection efc = link.Payload as EfConnection;
            if (efc != null)
            {
                efc.TargetId = link.ResolvedId;
                resConnections.Add(efc);
            }
            else
            {
                EfPlaceLink efl = link.Payload as EfPlaceLink;
                efl.TargetId = link.ResolvedId;
                resLinks.Add(efl);
            }
        }

        _context.Connections.AddRange(resConnections);
        _context.SaveChanges();

        _context.PlaceLinks.AddRange(resLinks);
        _context.SaveChanges();
    }

    /// <summary>
    /// Flushes this writer.
    /// </summary>
    public void Flush()
    {
        if (_places.Count == 0) return;

        _context.Places.AddRange(_places);
        _context.SaveChanges();
        _places.Clear();
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing) Flush();
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
