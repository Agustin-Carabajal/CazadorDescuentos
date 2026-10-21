using CazadorDescuentos.Modules.CatalogAndUserExperience.Models;
using CazadorDescuentos.Modules.IngestionAndAI.Models;
using Microsoft.EntityFrameworkCore;

namespace CazadorDescuentos.Shared.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<UserReport> UserReports => Set<UserReport>();
    public DbSet<RawScrape> RawScrapes => Set<RawScrape>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Offer>(entity =>
        {
            entity.ToTable("Offers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Brand).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(250).IsRequired();
            entity.Property(e => e.BenefitType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Channel).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DestinationUrl).HasMaxLength(500).IsRequired();

            entity.HasIndex(e => e.Brand);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.ValidUntil);

            entity.HasMany(e => e.UserReports)
                  .WithOne(r => r.Offer)
                  .HasForeignKey(r => r.OfferId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserReport>(entity =>
        {
            entity.ToTable("UserReports");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Comments).HasMaxLength(500);
            entity.Property(e => e.ReporterIp).HasMaxLength(50);
        });

        modelBuilder.Entity<RawScrape>(entity =>
        {
            entity.ToTable("RawScrapes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Brand).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SourceUrl).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.IsProcessed);
        });
    }
}
