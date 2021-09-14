using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PleiadesApi.Models;

namespace PleiadesApi.Services
{
    /// <summary>
    /// Application identity DB context.
    /// </summary>
    /// <seealso cref="IdentityDbContext" />
    public sealed class ApplicationDbContext :
        IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDbContext"/>
        /// class.
        /// </summary>
        /// <param name="options">The options.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // https://andrewlock.net/customising-asp-net-core-identity-ef-core-naming-conventions-for-postgresql/
        // https://github.com/efcore/EFCore.NamingConventions
        // https://stackoverflow.com/questions/59286100/aspnetcore-identity-using-postgresql-how-to-create-structure

        /// <summary>
        /// <para>
        /// Override this method to configure the database (and other options)
        /// to be used for this context.
        /// This method is called for each instance of the context that is created.
        /// The base implementation does nothing.
        /// </para>
        /// <para>
        /// In situations where an instance of <see cref="T:Microsoft.EntityFrameworkCore.DbContextOptions" />
        /// may or may not have been passed to the constructor, you can use
        /// <see cref="P:Microsoft.EntityFrameworkCore.DbContextOptionsBuilder.IsConfigured" />
        /// to determine if the options have already been set, and skip some
        /// or all of the logic in
        /// <see cref="M:Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)" />.
        /// </para>
        /// </summary>
        /// <param name="optionsBuilder">A builder used to create or modify
        /// options for this context. Databases (and other extensions)
        /// typically define extension methods on this object that allow you to
        /// configure the context.</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSnakeCaseNamingConvention();

        /// <summary>
        /// Override this method to further configure the model that was
        /// discovered by convention from the entity types
        /// exposed in <see cref="T:Microsoft.EntityFrameworkCore.DbSet`1" />
        /// properties on your derived context. The resulting model may be cached
        /// and re-used for subsequent instances of your derived context.
        /// </summary>
        /// <param name="modelBuilder">The builder being used to construct the
        /// model for this context. Databases (and other extensions) typically
        /// define extension methods on this object that allow you to configure
        /// aspects of the model that are specific to a given database.</param>
        /// <remarks>
        /// If a model is explicitly set on the options for this context (via
        /// <see cref="M:MicrosoftDbContextOptionsBuilder.UseModel(Microsoft.EntityFrameworkCore.Metadata.IModel)" />)
        /// then this method will not be run.
        /// </remarks>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // PostgreSQL uses the public schema by default - not dbo
            modelBuilder.HasDefaultSchema("public");
            base.OnModelCreating(modelBuilder);

            // rename identity tables
            modelBuilder.Entity<ApplicationUser>(b =>
            {
                b.ToTable("app_user");
            });

            modelBuilder.Entity<IdentityUserClaim<string>>(b =>
            {
                b.ToTable("app_user_claim");
            });

            modelBuilder.Entity<IdentityUserLogin<string>>(b =>
            {
                b.ToTable("app_user_login");
            });

            modelBuilder.Entity<IdentityUserToken<string>>(b =>
            {
                b.ToTable("app_user_token");
            });

            modelBuilder.Entity<ApplicationRole>(b =>
            {
                b.ToTable("app_role");
            });

            modelBuilder.Entity<IdentityRoleClaim<string>>(b =>
            {
                b.ToTable("app_role_claim");
            });

            modelBuilder.Entity<IdentityUserRole<string>>(b =>
            {
                b.ToTable("app_user_role");
            });

            // rename Identity tables to lowercase
            //foreach (var entity in modelBuilder.Model.GetEntityTypes())
            //{
            //    var currentTableName = modelBuilder.Entity(entity.Name)
            //        .Metadata.GetDefaultTableName();
            //    modelBuilder.Entity(entity.Name).ToTable(
            //        currentTableName.ToLowerInvariant());
            //}
        }
    }
}
