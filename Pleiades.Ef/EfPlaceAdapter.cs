using Pleiades.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pleiades.Ef;

public sealed class EfPlaceAdapter
{
    private readonly Dictionary<string, EfAuthor> _authors;

    /// <summary>
    /// Gets the lookup set.
    /// </summary>
    public LookupEntrySet LookupSet { get; }

    /// <summary>
    /// Gets the authors being tracked in this adapter.
    /// </summary>
    public IList<EfAuthor> GetAuthors() => _authors.Values.ToList();

    /// <summary>
    /// Initializes a new instance of the <see cref="EfPlaceAdapter"/> class.
    /// </summary>
    /// <param name="lookupSet">The lookup set.</param>
    /// <exception cref="ArgumentNullException">lookupSet</exception>
    public EfPlaceAdapter(LookupEntrySet lookupSet)
    {
        LookupSet = lookupSet
            ?? throw new ArgumentNullException(nameof(lookupSet));
        _authors = new Dictionary<string, EfAuthor>();
    }

    private static PendingLink GetPendingLink(string placeId, string targetId)
    {
        return new PendingLink(
            placeId, targetId,
            new EfPlaceLink
            {
                SourceId = placeId,
                TargetId = null,
            });
    }

    #region Author
    private static string BuildIdFromAuthorName(string name)
    {
        StringBuilder sb = new();
        foreach (char c in name.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c)) sb.Append(c);
        }
        sb.Insert(0, '#');
        return sb.ToString();
    }

    private EfAuthor GetAuthor(Author author)
    {
        // an author might already be existing. This is the only shared
        // record. All the others descend from each node's hierarchy and
        // are unique to each node.
        // Note that in some cases the username is null but his name is set.
        // In this case we create a fake username prefixed with #.
        string id = string.IsNullOrEmpty(author.Id)
            ? BuildIdFromAuthorName(author.Name ?? "")
            : author.Id;

        if (!_authors.ContainsKey(id))
        {
            _authors[id] = new EfAuthor
            {
                Id = id,
                Name = author.Name ?? "",
                Homepage = author.Homepage
            };
        }
        return _authors[id];
    }
    #endregion

    #region Connection
    private EfConnectionAttestation GetConnectionAttestation(
        Attestation attestation, EfConnection connection)
    {
        return new EfConnectionAttestation
        {
            PeriodId = LookupSet.GetId(attestation.PeriodId),
            ConfidenceId = LookupSet.GetId(attestation.ConfidenceId),
            // TODO: calculate rank
            Rank = 0,
            Connection = connection
        };
    }

    private EfConnectionReference GetConnectionReference(
        Reference reference, EfConnection connection)
    {
        return new EfConnectionReference
        {
            Title = reference.Title,
            TypeId = LookupSet.GetId(reference.Type, null, LookupEntrySet.REFTYPE_GROUP),
            CitTypeUriId = LookupSet.GetId(reference.CitTypeUri, null,
                LookupEntrySet.REFCITTYPE_GROUP),
            AccessUri = reference.AccessUri,
            AlternateUri = reference.AlternateUri,
            BibUri = reference.BibUri,
            Citation = reference.Citation,
            CitationDetail = reference.CitationDetail,
            OtherId = reference.OtherId,
            Connection = connection
        };
    }

    private PendingLink GetLinkForConnection(
        Connection connection, string sourcePlaceId)
    {
        EfConnection ef = new()
        {
            Uri = connection.Uri,
            Title = connection.Title,
            Description = connection.Description,
            StartYear = connection.StartYear,
            EndYear = connection.EndYear,
            Details = connection.Details,
            Provenance = connection.Provenance,
            CertaintyId = LookupSet.GetId(connection.Certainty, null,
                LookupEntrySet.CONNCERT_GROUP),
            TargetUri = connection.TargetUri,
            Created = connection.Created,
            Modified = connection.Modified,
            TypeId = LookupSet.GetId(connection.TypeId),
            ReviewStateId = LookupSet.GetId(connection.ReviewState,
                null, LookupEntrySet.STATE_GROUP),
            SourceId = sourcePlaceId,
            TargetId = null     // unknown, will be set later
        };

        // creators, contributors
        if (connection.Creators?.Count > 0)
        {
            ef.Authors = new List<EfConnectionAuthorLink>();
            foreach (var author in connection.Creators)
            {
                var efa = GetAuthor(author);
                ef.Authors.Add(new EfConnectionAuthorLink
                {
                    Connection = ef,
                    Author = efa,
                    Role = 'A'
                });
            }
        }

        if (connection.Contributors?.Count > 0)
        {
            ef.Authors ??= new List<EfConnectionAuthorLink>();
            foreach (var author in connection.Contributors)
            {
                var efa = GetAuthor(author);
                ef.Authors.Add(new EfConnectionAuthorLink
                {
                    Connection = ef,
                    Author = efa,
                    Role = 'C'
                });
            }
        }

        // attestation, references
        if (connection.Attestations?.Count > 0)
        {
            ef.Attestations = new List<EfConnectionAttestation>(
                connection.Attestations.Select(
                    a => GetConnectionAttestation(a, ef)));
        }

        if (connection.References?.Count > 0)
        {
            ef.References = new List<EfConnectionReference>(
                connection.References.Select(
                    r => GetConnectionReference(r, ef)));
        }

        return new PendingLink(sourcePlaceId, connection.TargetUri!, ef);
    }
    #endregion

    #region Location
    private EfLocationAttestation GetLocationAttestation(
        Attestation attestation, EfLocation location)
    {
        // all the location's attestations will be new records,
        // as they specifically belong to that location. So we don't
        // need to check for duplicate inserts.

        return new EfLocationAttestation
        {
            PeriodId = LookupSet.GetId(attestation.PeriodId),
            ConfidenceId = LookupSet.GetId(attestation.ConfidenceId),
            // TODO: set rank according to confidence ID
            Rank = 0,
            Location = location
        };
    }

    private EfLocationReference GetLocationReference(
        Reference reference, EfLocation location)
    {
        return new EfLocationReference
        {
            Title = reference.Title,
            TypeId = LookupSet.GetId(reference.Type, null,
                LookupEntrySet.REFTYPE_GROUP),
            CitTypeUriId = LookupSet.GetId(reference.CitTypeUri,
                null, LookupEntrySet.REFCITTYPE_GROUP),
            AccessUri = reference.AccessUri,
            AlternateUri = reference.AlternateUri,
            BibUri = reference.BibUri,
            Citation = reference.Citation,
            CitationDetail = reference.CitationDetail,
            OtherId = reference.OtherId,
            Location = location
        };
    }

    private EfLocation GetLocation(Location location, EfPlace place)
    {
        EfLocation ef = new()
        {
            Uri = location.Uri,
            StartYear = location.StartYear,
            EndYear = location.EndYear,
            Title = location.Title,
            Provenance = location.Provenance,
            Remains = location.Remains,
            Details = location.Details,
            AccuracyValue = location.AccuracyValue,
            Description = location.Description,
            Created = location.Created,
            Modified = location.Modified,
            Geometry = location.Geometry,
            Place = place,
            CertaintyId = LookupSet.GetId(location.CertaintyId),
            AccuracyId = LookupSet.GetId(location.AccuracyId),
            ReviewStateId = LookupSet.GetId(location.ReviewState, null,
                LookupEntrySet.STATE_GROUP)
        };

        // creators, contributors
        if (location.Creators?.Count > 0)
        {
            ef.Authors = new List<EfLocationAuthorLink>();
            foreach (var author in location.Creators)
            {
                var efa = GetAuthor(author);
                ef.Authors.Add(new EfLocationAuthorLink
                {
                    Location = ef,
                    Author = efa,
                    Role = 'A'
                });
            }
        }

        if (location.Contributors?.Count > 0)
        {
            ef.Authors ??= new List<EfLocationAuthorLink>();

            foreach (var author in location.Contributors)
            {
                var efa = GetAuthor(author);
                ef.Authors.Add(new EfLocationAuthorLink
                {
                    Location = ef,
                    Author = efa,
                    Role = 'C'
                });
            }
        }

        // attestation, references
        if (location.Attestations?.Count > 0)
        {
            ef.Attestations = new List<EfLocationAttestation>(
                location.Attestations.Select(
                    a => GetLocationAttestation(a, ef)));
        }

        if (location.References?.Count > 0)
        {
            ef.References = new List<EfLocationReference>(
                location.References.Select(
                    r => GetLocationReference(r, ef)));
        }

        // metadata come from:
        // featureType[] (dropped), featureTypeURI[],
        // locationType[] (dropped), locationTypeURI[]
        if (location.Metadata?.Count > 0)
        {
            ef.Metadata = new List<EfLocationMeta>(
                location.Metadata.Select(m => new EfLocationMeta
                {
                    Location = ef,
                    Name = m.Name,
                    Value = m.Value
                }));
        }

        return ef;
    }
    #endregion

    #region Name
    private EfNameAttestation GetNameAttestation(Attestation attestation,
        EfName name)
    {
        return new EfNameAttestation
        {
            PeriodId = LookupSet.GetId(attestation.PeriodId),
            ConfidenceId = LookupSet.GetId(attestation.ConfidenceId),
            // TODO: calculate rank
            Rank = 0,
            Name = name
        };
    }

    private EfNameReference GetNameReference(
        Reference reference, EfName name)
    {
        return new EfNameReference
        {
            Title = reference.Title,
            TypeId = LookupSet.GetId(reference.Type, null,
                LookupEntrySet.REFTYPE_GROUP),
            CitTypeUriId = LookupSet.GetId(reference.CitTypeUri, null,
                LookupEntrySet.REFCITTYPE_GROUP),
            AccessUri = reference.AccessUri,
            AlternateUri = reference.AlternateUri,
            BibUri = reference.BibUri,
            Citation = reference.Citation,
            CitationDetail = reference.CitationDetail,
            OtherId = reference.OtherId,
            Name = name
        };
    }

    private EfName GetName(Name name, EfPlace place)
    {
        EfName ef = new()
        {
            Place = place,
            CertaintyId = LookupSet.GetId(name.CertaintyId),
            ReviewStateId = LookupSet.GetId(name.ReviewState, null,
                LookupEntrySet.STATE_GROUP),
            TypeId = LookupSet.GetId(name.Type, null, LookupEntrySet.NAMETYPE_GROUP),
            Uri = name.Uri,
            Language = name.Language ?? "",
            StartYear = name.StartYear,
            EndYear = name.EndYear,
            Attested = name.Attested,
            Romanized = name.Romanized,
            Provenance = name.Provenance,
            Description = name.Description,
            Details = name.Details,
            TrAccuracyId = LookupSet.GetId(name.TrAccuracy, null,
                LookupEntrySet.NAMETRAC_GROUP),
            TrCompletenessId = LookupSet.GetId(name.TrCompleteness, null,
                LookupEntrySet.NAMETRCP_GROUP),
            Created = name.Created,
            Modified = name.Modified
        };

        // creators, contributors
        if (name.Creators?.Count > 0)
        {
            ef.Authors = new List<EfNameAuthorLink>();
            foreach (var author in name.Creators)
            {
                var efa = GetAuthor(author);
                ef.Authors.Add(new EfNameAuthorLink
                {
                    Name = ef,
                    Author = efa,
                    Role = 'A'
                });
            }
        }

        if (name.Contributors?.Count > 0)
        {
            if (ef.Authors == null)
                ef.Authors = new List<EfNameAuthorLink>();
            foreach (var author in name.Contributors)
            {
                var efa = GetAuthor(author);
                ef.Authors.Add(new EfNameAuthorLink
                {
                    Name = ef,
                    Author = efa,
                    Role = 'C'
                });
            }
        }

        // attestation, references
        if (name.Attestations?.Count > 0)
        {
            ef.Attestations = new List<EfNameAttestation>(
                name.Attestations.Select(
                    a => GetNameAttestation(a, ef)));
        }

        if (name.References?.Count > 0)
        {
            ef.References = new List<EfNameReference>(
                name.References.Select(
                    r => GetNameReference(r, ef)));
        }

        return ef;
    }
    #endregion

    #region Place
    private EfPlaceFeature GetPlaceFeature(PlaceFeature feature,
        EfPlace place)
    {
        return new EfPlaceFeature
        {
            Type = feature.Type,
            Title = feature.Title,
            Geometry = feature.Geometry,
            Snippet = feature.Snippet,
            Link = feature.Link,
            Description = feature.Description,
            Precision = feature.Precision,
            Place = place
        };
    }

    private EfPlaceAttestation GetPlaceAttestation(
        Attestation attestation, EfPlace place)
    {
        return new EfPlaceAttestation
        {
            PeriodId = LookupSet.GetId(attestation.PeriodId),
            ConfidenceId = LookupSet.GetId(attestation.ConfidenceId),
            // TODO: set rank according to confidence ID
            Rank = 0,
            Place = place
        };
    }

    private EfPlaceReference GetPlaceReference(
        Reference reference, EfPlace place)
    {
        return new EfPlaceReference
        {
            Title = reference.Title,
            TypeId = LookupSet.GetId(reference.Type, null, LookupEntrySet.REFTYPE_GROUP),
            CitTypeUriId = LookupSet.GetId(reference.CitTypeUri, null,
                LookupEntrySet.REFCITTYPE_GROUP),
            AccessUri = reference.AccessUri,
            AlternateUri = reference.AlternateUri,
            BibUri = reference.BibUri,
            Citation = reference.Citation,
            CitationDetail = reference.CitationDetail,
            OtherId = reference.OtherId,
            Place = place
        };
    }

    public Tuple<EfPlace,List<PendingLink>> GetPlace(
        Place source, PlaceChildFlags flags)
    {
        List<PendingLink> links = new();

        EfPlace target = new()
        {
            Id = source.Id,
            Uri = source.Uri,
            Title = source.Title,
            Description = source.Description,
            Details = source.Details,
            Provenance = source.Provenance,
            Rights = source.Rights,
            Created = source.Created,
            Modified = source.Modified,
            RpLat = source.RpLat,
            RpLon = source.RpLon,
            BboxSwLat = source.BboxSwLat,
            BboxSwLon = source.BboxSwLon,
            BboxNeLat = source.BboxNeLat,
            BboxNeLon = source.BboxNeLon,
            ReviewStateId = LookupSet.GetId(source.ReviewState, null,
                LookupEntrySet.STATE_GROUP)
        };

        // features
        if ((flags & PlaceChildFlags.Features) != 0
            && source.Features?.Count > 0)
        {
            target.Features = new List<EfPlaceFeature>(
                source.Features.Select(f => GetPlaceFeature(f, target)));
        }

        // creators, contributors
        if ((flags & PlaceChildFlags.Creators) != 0
            && source.Creators?.Count > 0)
        {
            target.Authors = new List<EfPlaceAuthorLink>();
            foreach (var author in source.Creators)
            {
                var efa = GetAuthor(author);
                target.Authors.Add(new EfPlaceAuthorLink
                {
                    Place = target,
                    Author = efa,
                    Role = 'A'
                });
            }
        }

        if ((flags & PlaceChildFlags.Contributors) != 0
            && source.Contributors?.Count > 0)
        {
            if (target.Authors == null)
                target.Authors = new List<EfPlaceAuthorLink>();

            foreach (var author in source.Contributors)
            {
                var efa = GetAuthor(author);
                target.Authors.Add(new EfPlaceAuthorLink
                {
                    Place = target,
                    Author = efa,
                    Role = 'C'
                });
            }
        }

        // locations
        if ((flags & PlaceChildFlags.Locations) != 0
            && source.Locations?.Count > 0)
        {
            target.Locations = new List<EfLocation>(
                source.Locations.Select(l => GetLocation(l, target)));
        }

        // connections
        if ((flags & PlaceChildFlags.Connections) != 0
            && source.Connections?.Count > 0)
        {
            target.SourceConnections = new List<EfConnection>();
            foreach (var connection in source.Connections)
            {
                // place connections require both source and target place
                // to be already present in the DB. Here we just defer
                // the place lookup, returning a pending link for each connection
                links.Add(GetLinkForConnection(connection, source.Id!));
            }
        }

        // attestation, references
        if ((flags & PlaceChildFlags.Attestations) != 0
            && source.Attestations?.Count > 0)
        {
            target.Attestations = new List<EfPlaceAttestation>(
                source.Attestations.Select(
                    a => GetPlaceAttestation(a, target)));
        }

        if ((flags & PlaceChildFlags.References) != 0
            && source.References?.Count > 0)
        {
            target.References = new List<EfPlaceReference>(
                source.References.Select(r => GetPlaceReference(r, target)));
        }

        // names
        if ((flags & PlaceChildFlags.Names) != 0
            && source.Names?.Count > 0)
        {
            target.Names = new List<EfName>(
                source.Names.Select(n => GetName(n, target)));
        }

        // metadata
        if ((flags & PlaceChildFlags.Metadata) != 0
            && source.Metadata?.Count > 0)
        {
            target.Metadata = new List<EfPlaceMeta>(
                source.Metadata.Select(m => new EfPlaceMeta
                {
                    Place = target,
                    Name = m.Name,
                    Value = m.Value
                }));
        }

        // target URIs become PlaceLink
        // (assuming that all these URIs target places...TODO: verify)
        if ((flags & PlaceChildFlags.TargetUris) != 0
            && source.TargetUris?.Count > 0)
        {
            for (int i = 0; i < source.TargetUris.Count; i++)
            {
                // check that the URI is not duplicated by error,
                // like in [169] 266521
                string uri = source.TargetUris[i];
                if (source.TargetUris.IndexOf(uri, 0, i) > -1)
                    continue;

                links.Add(GetPendingLink(source.Id!, uri));
                i++;
            }
        }

        return Tuple.Create(target, links);
    }
    #endregion
}

#region Flags
[Flags]
public enum PlaceChildFlags
{
    None = 0,
    Features = 0x001,
    Creators = 0x002,
    Contributors = 0x004,
    Locations = 0x008,
    Connections = 0x010,
    Attestations = 0x020,
    References = 0x040,
    Names = 0x080,
    Metadata = 0x100,
    TargetUris = 0x200,
    All = Features | Creators | Contributors | Locations | Connections
        | Attestations | References | Names | Metadata | TargetUris
}
#endregion

#region PendingLink
/// <summary>
/// A pending link, to be resolved when all the places have been read.
/// Pending links can be generated from place's connections or connectsTo
/// properties, and refer to URIs. Internally we map them to IDs, which
/// are shorter.
/// </summary>
public sealed class PendingLink
{
    /// <summary>
    /// Gets the source place identifier.
    /// </summary>
    public string SourceId { get; }

    /// <summary>
    /// Gets the payload EF object representing the link, which should later
    /// be stored once resolved.
    /// </summary>
    public object Payload { get; }

    /// <summary>
    /// Gets the original target identifier (usually a URI).
    /// </summary>
    public string OriginalId { get; }

    /// <summary>
    /// Gets or sets the resolved target identifier. This is initially null,
    /// and then set as soon as the place whose URI matches
    /// <see cref="SourceId"/> is read. So, at the end of the read process
    /// all the pending links should have been resolved, and can be written.
    /// </summary>
    public string ResolvedId { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PendingLink"/> class.
    /// </summary>
    /// <param name="sourceId">The source identifier.</param>
    /// <param name="originalId">The original identifier.</param>
    /// <param name="payload">The payload.</param>
    public PendingLink(string sourceId, string originalId, object payload)
    {
        SourceId = sourceId;
        OriginalId = originalId;
        Payload = payload;
    }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{SourceId} => {ResolvedId ?? "?"}";
    }
}
#endregion
