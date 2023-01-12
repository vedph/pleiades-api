using Microsoft.EntityFrameworkCore;

namespace Pleiades.Ef;

/// <summary>
/// EF DB context.
/// </summary>
/// <remarks>This is a generic blueprint. Instantiate like:
/// <code>
/// services.AddDbContext&lt;CorpusDbContext&gt;(
///   options => options.Use...(connStr));
/// </code>
/// </remarks>
/// <seealso cref="DbContext" />
public sealed class PleiadesDbContext : DbContext
{
    public DbSet<EfLookup> Lookups { get; set; }
    public DbSet<EfAuthor> Authors { get; set; }
    public DbSet<EfPlace> Places { get; set; }
    public DbSet<EfPlaceFeature> PlaceFeatures { get; set; }
    public DbSet<EfPlaceMeta> PlaceMetas { get; set; }
    public DbSet<EfPlaceAuthorLink> PlaceAuthorLinks { get; set; }
    public DbSet<EfPlaceLink> PlaceLinks { get; set; }
    public DbSet<EfPlaceAttestation> PlaceAttestations { get; set; }
    public DbSet<EfPlaceReference> PlaceReferences { get; set; }
    public DbSet<EfConnection> Connections { get; set; }
    public DbSet<EfConnectionAuthorLink> ConnectionAuthorLinks { get; set; }
    public DbSet<EfConnectionReference> ConnectionReferences { get; set; }
    public DbSet<EfConnectionAttestation> ConnectionAttestations { get; set; }
    public DbSet<EfLocation> Locations { get; set; }
    public DbSet<EfLocationAuthorLink> LocationAuthorLinks { get; set; }
    public DbSet<EfLocationAttestation> LocationAttestations { get; set; }
    public DbSet<EfLocationMeta> LocationMetas { get; set; }
    public DbSet<EfLocationReference> LocationReferences { get; set; }
    public DbSet<EfName> Names { get; set; }
    public DbSet<EfNameAuthorLink> NameAuthorLinks { get; set; }
    public DbSet<EfNameAttestation> NameAttestations { get; set; }
    public DbSet<EfNameReference> NameReferences { get; set; }
    public DbSet<EfToken> Tokens { get; set; }

    // https://docs.microsoft.com/en-us/ef/core/miscellaneous/configuring-dbcontext

    /// <summary>
    /// Initializes a new instance of the <see cref="PleiadesDbContext"/> class.
    /// </summary>
    public PleiadesDbContext()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PleiadesDbContext"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    public PleiadesDbContext(DbContextOptions<PleiadesDbContext> options) :
        base(options)
    {
        // https://stackoverflow.com/questions/42616408/entity-framework-core-multiple-connection-strings-on-same-dbcontext
    }

    /// <summary>
    /// <para>
    /// Override this method to configure the database (and other options)
    /// to be used for this context.
    /// This method is called for each instance of the context that is
    /// created.
    /// </para>
    /// <para>
    /// In situations where an instance of <see cref="DbContextOptions" />
    /// may or may not have been passed to the constructor, you can use
    /// <see cref="DbContextOptionsBuilder.IsConfigured" /> to determine if
    /// the options have already been set, and skip some or all of the logic
    /// in <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)" />.
    /// </para>
    /// </summary>
    /// <param name="optionsBuilder">A builder used to create or modify
    /// options for this context. Databases (and other extensions)
    /// typically define extension methods on this object that allow you
    /// to configure the context.</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
#if DEBUG
        optionsBuilder.EnableSensitiveDataLogging(true);
#endif
        // https://github.com/efcore/EFCore.NamingConventions
        optionsBuilder.UseSnakeCaseNamingConvention();

        // base.OnConfiguring(optionsBuilder)
    }

    /// <summary>
    /// Override this method to further configure the model that was
    /// discovered by convention from the entity types exposed in
    /// <see cref="DbSet`1" /> properties on your derived context.
    /// The resulting model may be cached and re-used for subsequent
    /// instances of your derived context.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct
    /// the model for this context. Databases (and other extensions)
    /// typically define extension methods on this object that allow you
    /// to configure aspects of the model that are specific to a given
    /// database.</param>
    /// <remarks>
    /// If a model is explicitly set on the options for this context
    /// (via <see cref="DbContextOptionsBuilder.UseModel(IModel)" />)
    /// then this method will not be run.
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // lookup
        modelBuilder.Entity<EfLookup>().ToTable("lookup");
        modelBuilder.Entity<EfLookup>().Property(p => p.Id)
            .IsRequired();
        modelBuilder.Entity<EfLookup>().Property(p => p.Group)
            .HasMaxLength(50);
        modelBuilder.Entity<EfLookup>().Property(p => p.FullName)
            .IsRequired()
            .HasMaxLength(200);
        modelBuilder.Entity<EfLookup>().Property(p => p.ShortName)
            .IsRequired()
            .HasMaxLength(100);

        // author
        modelBuilder.Entity<EfAuthor>().ToTable("author");
        modelBuilder.Entity<EfAuthor>().Property(a => a.Id)
            .IsRequired()
            .HasMaxLength(50);
        modelBuilder.Entity<EfAuthor>().Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(100);
        modelBuilder.Entity<EfAuthor>().Property(a => a.Homepage)
            .HasMaxLength(200);

        // place
        modelBuilder.Entity<EfPlace>().ToTable("place");
        modelBuilder.Entity<EfPlace>().Property(p => p.Id)
            .IsRequired()
            .HasMaxLength(20);
        modelBuilder.Entity<EfPlace>().Property(p => p.Uri)
            .IsRequired()
            .HasMaxLength(200);
        modelBuilder.Entity<EfPlace>().Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(200);
        modelBuilder.Entity<EfPlace>().Property(p => p.Description)
            .HasMaxLength(5000);
        // details is TEXT
        modelBuilder.Entity<EfPlace>().Property(p => p.Provenance)
            .HasMaxLength(500);
        modelBuilder.Entity<EfPlace>().Property(p => p.Rights)
            .HasMaxLength(500);
        modelBuilder.Entity<EfPlace>().Property(p => p.ReviewStateId)
            .IsRequired();
        modelBuilder.Entity<EfPlace>().Property(p => p.Created)
            .IsRequired();
        modelBuilder.Entity<EfPlace>().Property(p => p.Modified)
            .IsRequired();
        modelBuilder.Entity<EfPlace>().Property(p => p.RpLat).IsRequired();
        modelBuilder.Entity<EfPlace>().Property(p => p.RpLon).IsRequired();
        // bbox... are optional

        // place_feature
        modelBuilder.Entity<EfPlaceFeature>().ToTable("place_feature");
        modelBuilder.Entity<EfPlaceFeature>().Property(f => f.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<EfPlaceFeature>().Property(f => f.PlaceId)
            .IsRequired()
            .HasMaxLength(20);
        modelBuilder.Entity<EfPlaceFeature>().Property(f => f.Type)
            .IsRequired()
            .HasMaxLength(50);
        modelBuilder.Entity<EfPlaceFeature>().Property(f => f.Title)
            .IsRequired()
            .HasMaxLength(200);
        modelBuilder.Entity<EfPlaceFeature>().Property(f => f.Geometry)
            .IsRequired()
            .HasMaxLength(2000);
        modelBuilder.Entity<EfPlaceFeature>().Property(f => f.Snippet)
            .HasMaxLength(100);
        modelBuilder.Entity<EfPlaceFeature>().Property(f => f.Link)
            .HasMaxLength(200);
        modelBuilder.Entity<EfPlaceFeature>().Property(f => f.Description)
            .HasMaxLength(500);
        modelBuilder.Entity<EfPlaceFeature>().Property(f => f.Precision)
            .HasMaxLength(50);

        // place_meta
        modelBuilder.Entity<EfPlaceMeta>().ToTable("place_meta");
        modelBuilder.Entity<EfPlaceMeta>().Property(m => m.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<EfPlaceMeta>().Property(m => m.PlaceId)
            .IsRequired()
            .HasMaxLength(20);
        modelBuilder.Entity<EfPlaceMeta>().Property(m => m.Name)
            .HasMaxLength(100);
        modelBuilder.Entity<EfPlaceMeta>().Property(m => m.Value)
            .HasMaxLength(500);

        // place_author_link
        modelBuilder.Entity<EfPlaceAuthorLink>().ToTable("place_author_link");
        modelBuilder.Entity<EfPlaceAuthorLink>().Property(l => l.PlaceId)
            .IsRequired();
        modelBuilder.Entity<EfPlaceAuthorLink>().Property(l => l.AuthorId)
            .IsRequired();
        modelBuilder.Entity<EfPlaceAuthorLink>().HasKey(l => new
        {
            l.PlaceId,
            l.AuthorId,
            l.Role
        });

        // place_link
        modelBuilder.Entity<EfPlaceLink>().ToTable("place_link");
        modelBuilder.Entity<EfPlaceLink>().Property(l => l.SourceId)
            .HasMaxLength(20)
            .IsRequired();
        modelBuilder.Entity<EfPlaceLink>().Property(l => l.TargetId)
            .HasMaxLength(20)
            .IsRequired();
        modelBuilder.Entity<EfPlaceLink>().HasKey(l => new
        {
            l.SourceId,
            l.TargetId
        });

        // place_attestation
        modelBuilder.Entity<EfPlaceAttestation>()
            .ToTable("place_attestations");
        modelBuilder.Entity<EfPlaceAttestation>().Property(a => a.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<EfPlaceAttestation>()
            .Property(a => a.PlaceId)
            .IsRequired();
        modelBuilder.Entity<EfPlaceAttestation>()
            .Property(a => a.PeriodId).IsRequired();
        modelBuilder.Entity<EfPlaceAttestation>()
            .Property(a => a.ConfidenceId).IsRequired();
        modelBuilder.Entity<EfPlaceAttestation>()
            .Property(a => a.Rank).IsRequired();

        // place_reference
        modelBuilder.Entity<EfPlaceReference>().ToTable("place_reference");
        modelBuilder.Entity<EfPlaceReference>().Property(r => r.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<EfPlaceReference>().Property(r => r.PlaceId)
            .IsRequired()
            .HasMaxLength(20);
        modelBuilder.Entity<EfPlaceReference>().Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(300);
        modelBuilder.Entity<EfPlaceReference>().Property(r => r.TypeId)
            .IsRequired();
        modelBuilder.Entity<EfPlaceReference>().Property(r => r.CitTypeUriId)
            .IsRequired();
        modelBuilder.Entity<EfPlaceReference>().Property(r => r.AccessUri)
            .HasMaxLength(1000);
        modelBuilder.Entity<EfPlaceReference>().Property(r => r.AlternateUri)
            .HasMaxLength(1000);
        modelBuilder.Entity<EfPlaceReference>().Property(r => r.BibUri)
            .HasMaxLength(1000);
        modelBuilder.Entity<EfPlaceReference>().Property(r => r.Citation)
            .HasMaxLength(100);
        modelBuilder.Entity<EfPlaceReference>().Property(r => r.CitationDetail)
            .HasMaxLength(500);
        modelBuilder.Entity<EfPlaceReference>().Property(r => r.OtherId)
            .HasMaxLength(500);

        // connection
        modelBuilder.Entity<EfConnection>().ToTable("connection");
        modelBuilder.Entity<EfConnection>().Property(c => c.TargetId)
            .IsRequired()
            .HasMaxLength(80);
        modelBuilder.Entity<EfConnection>().Property(c => c.SourceId)
            .IsRequired()
            .HasMaxLength(20);
        modelBuilder.Entity<EfConnection>().Property(c => c.TargetId)
            .IsRequired()
            .HasMaxLength(20);
        modelBuilder.Entity<EfConnection>().Property(c => c.Uri)
            .IsRequired()
            .HasMaxLength(200);
        modelBuilder.Entity<EfConnection>().Property(c => c.TypeId)
            .IsRequired();
        modelBuilder.Entity<EfConnection>().Property(c => c.CertaintyId)
            .IsRequired();
        modelBuilder.Entity<EfConnection>().Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(100);
        modelBuilder.Entity<EfConnection>().Property(c => c.Description)
            .HasMaxLength(500);
        modelBuilder.Entity<EfConnection>().Property(c => c.StartYear)
            .IsRequired();
        modelBuilder.Entity<EfConnection>().Property(c => c.EndYear)
            .IsRequired();
        modelBuilder.Entity<EfConnection>()
            .HasOne(c => c.Source).WithMany(p => p.SourceConnections);
        modelBuilder.Entity<EfConnection>()
            .HasOne(c => c.Target).WithMany(p => p.TargetConnections);

        // connection_author_link
        modelBuilder.Entity<EfConnectionAuthorLink>()
            .ToTable("connection_author_link");
        modelBuilder.Entity<EfConnectionAuthorLink>().Property(l => l.ConnectionId)
            .IsRequired();
        modelBuilder.Entity<EfConnectionAuthorLink>().Property(l => l.AuthorId)
            .IsRequired();
        modelBuilder.Entity<EfConnectionAuthorLink>().HasKey(l => new
        {
            l.ConnectionId,
            l.AuthorId,
            l.Role
        });

        // connection_reference
        modelBuilder.Entity<EfConnectionReference>().ToTable("connection_reference");
        modelBuilder.Entity<EfConnectionReference>().Property(r => r.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<EfConnectionReference>().Property(r => r.ConnectionId)
            .IsRequired()
            .HasMaxLength(80);
        modelBuilder.Entity<EfConnectionReference>().Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(300);
        modelBuilder.Entity<EfConnectionReference>().Property(r => r.TypeId)
            .IsRequired();
        modelBuilder.Entity<EfConnectionReference>().Property(r => r.CitTypeUriId)
            .IsRequired();
        modelBuilder.Entity<EfConnectionReference>().Property(r => r.AccessUri)
            .HasMaxLength(1000);
        modelBuilder.Entity<EfConnectionReference>().Property(r => r.AlternateUri)
            .HasMaxLength(1000);
        modelBuilder.Entity<EfConnectionReference>().Property(r => r.BibUri)
            .HasMaxLength(1000);
        modelBuilder.Entity<EfConnectionReference>().Property(r => r.Citation)
            .HasMaxLength(100);
        modelBuilder.Entity<EfConnectionReference>().Property(r => r.CitationDetail)
            .HasMaxLength(500);
        modelBuilder.Entity<EfConnectionReference>().Property(r => r.OtherId)
            .HasMaxLength(500);

        // connection_attestation
        modelBuilder.Entity<EfConnectionAttestation>()
            .ToTable("connection_attestation");
        modelBuilder.Entity<EfConnectionAttestation>().Property(a => a.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<EfConnectionAttestation>()
            .Property(a => a.ConnectionId)
            .IsRequired();
        modelBuilder.Entity<EfConnectionAttestation>()
            .Property(a => a.PeriodId).IsRequired();
        modelBuilder.Entity<EfConnectionAttestation>()
            .Property(a => a.ConfidenceId).IsRequired();
        modelBuilder.Entity<EfConnectionAttestation>()
            .Property(a => a.Rank).IsRequired();

        // location
        modelBuilder.Entity<EfLocation>().ToTable("location");
        modelBuilder.Entity<EfLocation>().Property(l => l.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<EfLocation>().Property(l => l.PlaceId)
            .IsRequired();
        modelBuilder.Entity<EfLocation>().Property(l => l.CertaintyId)
            .IsRequired();
        modelBuilder.Entity<EfLocation>().Property(l => l.AccuracyId)
            .IsRequired();
        modelBuilder.Entity<EfLocation>().Property(l => l.ReviewStateId)
            .IsRequired();
        modelBuilder.Entity<EfLocation>().Property(l => l.Uri)
            .IsRequired()
            .HasMaxLength(200);
        modelBuilder.Entity<EfLocation>().Property(l => l.StartYear)
            .IsRequired();
        modelBuilder.Entity<EfLocation>().Property(l => l.EndYear)
            .IsRequired();
        modelBuilder.Entity<EfLocation>().Property(l => l.Title)
            .IsRequired()
            .HasMaxLength(200);
        modelBuilder.Entity<EfLocation>().Property(l => l.Provenance)
            .HasMaxLength(500);
        modelBuilder.Entity<EfLocation>().Property(l => l.Remains)
            .HasMaxLength(500);
        modelBuilder.Entity<EfLocation>().Property(l => l.AccuracyValue)
            .IsRequired();
        modelBuilder.Entity<EfLocation>().Property(l => l.Description)
            .HasMaxLength(500);
        modelBuilder.Entity<EfLocation>().Property(l => l.Created)
            .IsRequired();
        modelBuilder.Entity<EfLocation>().Property(l => l.Modified)
            .IsRequired();
        modelBuilder.Entity<EfLocation>().Property(l => l.Geometry)
            .IsRequired()
            .HasMaxLength(2000);

        // location_author_link
        modelBuilder.Entity<EfLocationAuthorLink>()
            .ToTable("location_author_link");
        modelBuilder.Entity<EfLocationAuthorLink>().Property(l => l.LocationId)
            .IsRequired();
        modelBuilder.Entity<EfLocationAuthorLink>().Property(l => l.AuthorId)
            .IsRequired();
        modelBuilder.Entity<EfLocationAuthorLink>().HasKey(l => new
        {
            l.LocationId,
            l.AuthorId,
            l.Role
        });

        // location_attestation
        modelBuilder.Entity<EfLocationAttestation>()
            .ToTable("location_attestation");
        modelBuilder.Entity<EfLocationAttestation>().Property(a => a.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<EfLocationAttestation>()
            .Property(a => a.LocationId)
            .IsRequired();
        modelBuilder.Entity<EfLocationAttestation>()
            .Property(a => a.PeriodId).IsRequired();
        modelBuilder.Entity<EfLocationAttestation>()
            .Property(a => a.ConfidenceId).IsRequired();
        modelBuilder.Entity<EfLocationAttestation>()
            .Property(a => a.Rank).IsRequired();

        // location_meta
        modelBuilder.Entity<EfLocationMeta>().ToTable("location_meta");
        modelBuilder.Entity<EfLocationMeta>().Property(m => m.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<EfLocationMeta>().Property(m => m.LocationId)
            .IsRequired();
        modelBuilder.Entity<EfLocationMeta>().Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(100);
        modelBuilder.Entity<EfLocationMeta>().Property(m => m.Value)
            .IsRequired()
            .HasMaxLength(500);

        // location_reference
        modelBuilder.Entity<EfLocationReference>().ToTable("location_reference");
        modelBuilder.Entity<EfLocationReference>().Property(r => r.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<EfLocationReference>().Property(r => r.LocationId)
            .IsRequired();
        modelBuilder.Entity<EfLocationReference>().Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(300);
        modelBuilder.Entity<EfLocationReference>().Property(r => r.TypeId)
            .IsRequired();
        modelBuilder.Entity<EfLocationReference>().Property(r => r.CitTypeUriId)
            .IsRequired();
        modelBuilder.Entity<EfLocationReference>().Property(r => r.AccessUri)
            .HasMaxLength(1000);
        modelBuilder.Entity<EfLocationReference>().Property(r => r.AlternateUri)
            .HasMaxLength(1000);
        modelBuilder.Entity<EfLocationReference>().Property(r => r.BibUri)
            .HasMaxLength(1000);
        modelBuilder.Entity<EfLocationReference>().Property(r => r.Citation)
            .HasMaxLength(100);
        modelBuilder.Entity<EfLocationReference>().Property(r => r.CitationDetail)
            .HasMaxLength(500);
        modelBuilder.Entity<EfLocationReference>().Property(r => r.OtherId)
            .HasMaxLength(500);

        // name
        modelBuilder.Entity<EfName>().ToTable("name");
        modelBuilder.Entity<EfName>().Property(n => n.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<EfName>().Property(n => n.PlaceId)
            .IsRequired()
            .HasMaxLength(20);
        modelBuilder.Entity<EfName>().Property(n => n.CertaintyId)
            .IsRequired();
        modelBuilder.Entity<EfName>().Property(n => n.ReviewStateId)
            .IsRequired();
        modelBuilder.Entity<EfName>().Property(n => n.TypeId)
            .IsRequired();
        modelBuilder.Entity<EfName>().Property(n => n.Uri)
            .IsRequired()
            .HasMaxLength(200);
        modelBuilder.Entity<EfName>().Property(n => n.Language)
            .IsRequired()
            .HasMaxLength(50);
        modelBuilder.Entity<EfName>().Property(n => n.StartYear).IsRequired();
        modelBuilder.Entity<EfName>().Property(n => n.EndYear).IsRequired();
        modelBuilder.Entity<EfName>().Property(n => n.Attested)
            .HasMaxLength(100);
        modelBuilder.Entity<EfName>().Property(n => n.Romanized)
            .IsRequired()
            .HasMaxLength(500);
        modelBuilder.Entity<EfName>().Property(n => n.Provenance)
            .HasMaxLength(500);
        modelBuilder.Entity<EfName>().Property(n => n.Description)
            .HasMaxLength(500);
        modelBuilder.Entity<EfName>().Property(n => n.TrAccuracyId)
            .IsRequired();
        modelBuilder.Entity<EfName>().Property(n => n.TrCompletenessId)
            .IsRequired();
        modelBuilder.Entity<EfName>().Property(n => n.Created).IsRequired();
        modelBuilder.Entity<EfName>().Property(n => n.Modified).IsRequired();

        // name_author_link
        modelBuilder.Entity<EfNameAuthorLink>().ToTable("name_author_link");
        modelBuilder.Entity<EfNameAuthorLink>().Property(l => l.NameId)
            .IsRequired();
        modelBuilder.Entity<EfNameAuthorLink>().Property(l => l.AuthorId)
            .IsRequired();
        modelBuilder.Entity<EfNameAuthorLink>().HasKey(l => new
        {
            l.NameId,
            l.AuthorId,
            l.Role
        });

        // name_attestation
        modelBuilder.Entity<EfNameAttestation>()
            .ToTable("name_attestation");
        modelBuilder.Entity<EfNameAttestation>().Property(a => a.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<EfNameAttestation>()
            .Property(a => a.NameId)
            .IsRequired();
        modelBuilder.Entity<EfNameAttestation>()
            .Property(a => a.PeriodId).IsRequired();
        modelBuilder.Entity<EfNameAttestation>()
            .Property(a => a.ConfidenceId).IsRequired();
        modelBuilder.Entity<EfNameAttestation>()
            .Property(a => a.Rank).IsRequired();

        // name_reference
        modelBuilder.Entity<EfNameReference>().ToTable("name_reference");
        modelBuilder.Entity<EfNameReference>().Property(r => r.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<EfNameReference>().Property(r => r.NameId)
            .IsRequired();
        modelBuilder.Entity<EfNameReference>().Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(300);
        modelBuilder.Entity<EfNameReference>().Property(r => r.TypeId)
            .IsRequired();
        modelBuilder.Entity<EfNameReference>().Property(r => r.CitTypeUriId)
            .IsRequired();
        modelBuilder.Entity<EfNameReference>().Property(r => r.AccessUri)
            .HasMaxLength(1000);
        modelBuilder.Entity<EfNameReference>().Property(r => r.AlternateUri)
            .HasMaxLength(1000);
        modelBuilder.Entity<EfNameReference>().Property(r => r.BibUri)
            .HasMaxLength(1000);
        modelBuilder.Entity<EfNameReference>().Property(r => r.Citation)
            .HasMaxLength(100);
        modelBuilder.Entity<EfNameReference>().Property(r => r.CitationDetail)
            .HasMaxLength(500);
        modelBuilder.Entity<EfNameReference>().Property(r => r.OtherId)
            .HasMaxLength(500);

        // eix_token
        modelBuilder.Entity<EfToken>().ToTable("eix_token");
        modelBuilder.Entity<EfToken>().Property(r => r.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<EfToken>().Property(r => r.TargetId)
            .IsRequired()
            .HasMaxLength(20);
        modelBuilder.Entity<EfToken>().Property(r => r.Field)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(5)
            .IsFixedLength();
        modelBuilder.Entity<EfToken>().Property(r => r.Value)
            .IsRequired()
            .HasMaxLength(100);
        modelBuilder.Entity<EfToken>().Property(r => r.Rank)
            .IsRequired();
        modelBuilder.Entity<EfToken>().Property(r => r.Language)
            .HasMaxLength(5);
        modelBuilder.Entity<EfToken>().Property(r => r.YearMin).IsRequired();
        modelBuilder.Entity<EfToken>().Property(r => r.YearMax).IsRequired();

        // eix_occurrence
        modelBuilder.Entity<EfOccurrence>().ToTable("eix_occurrence");
        modelBuilder.Entity<EfOccurrence>().Property(r => r.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<EfOccurrence>().Property(r => r.TokenId)
            .IsRequired()
            .HasMaxLength(20);
        modelBuilder.Entity<EfOccurrence>().Property(r => r.Field)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(5)
            .IsFixedLength();
        modelBuilder.Entity<EfOccurrence>().Property(r => r.Rank)
            .IsRequired();
        modelBuilder.Entity<EfOccurrence>().Property(r => r.YearMin).IsRequired();
        modelBuilder.Entity<EfOccurrence>().Property(r => r.YearMax).IsRequired();

        base.OnModelCreating(modelBuilder);
    }
}