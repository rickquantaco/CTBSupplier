using CTBSupplier.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CTBSupplier.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<StockItem> StockItems { get; set; }
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<LoginHistory> LoginHistories { get; set; }
    public DbSet<AppPermission> AppPermissions { get; set; }
    public DbSet<AppUserPermission> AppUserPermissions { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<AppUserSupplier> AppUserSuppliers { get; set; }
    public DbSet<StockItemUnitsOfMeasurementAndPrice> StockItemUnitsOfMeasurementAndPrices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Supplier: single GUID PK
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("Supplier");
            entity.HasKey(e => e.SupplierGUID);
            entity.Property(e => e.SupplierGUID)
                  .HasColumnName("supplierGUID")
                  .ValueGeneratedOnAdd();
            entity.Property(e => e.SupplierName).HasColumnName("supplierName");
            entity.Property(e => e.SupplierAbn).HasColumnName("supplierAbn");
            entity.Property(e => e.IsActive).HasColumnName("isActive");
            entity.Property(e => e.Website).HasColumnName("website");
            entity.Property(e => e.SupplierImage).HasColumnName("supplierImage");
            entity.Property(e => e.SupplierDescription).HasColumnName("supplierDescription");
            entity.Property(e => e.SupplierCategory).HasColumnName("supplierCategory");
            entity.Property(e => e.DateTimeAddedUtc)
                  .HasColumnName("dateTimeAddedUTC")
                  .HasDefaultValueSql("GETUTCDATE()")
                  .ValueGeneratedOnAdd();
            entity.Property(e => e.SupplierAbnForLookups)
                  .HasColumnName("supplierAbnForLookups")
                  .HasComputedColumnSql("CAST(REPLACE(supplierAbn, ' ', '') AS nvarchar(20))", stored: true);
        });

        // StockItem: composite PK (supplierGUID, stockCode), supplierGUID also FK to Supplier
        modelBuilder.Entity<StockItem>(entity =>
        {
            entity.ToTable("StockItem");
            entity.HasKey(e => new { e.SupplierGUID, e.StockCode });
            entity.Property(e => e.SupplierGUID).HasColumnName("supplierGUID");
            entity.Property(e => e.StockCode).HasColumnName("stockCode");
            entity.Property(e => e.StockDesc).HasColumnName("stockDesc");
            entity.Property(e => e.BrandName).HasColumnName("brandName");
            entity.Property(e => e.SupplierStockCode).HasColumnName("supplierStockCode");
            entity.Property(e => e.StockCategoryName).HasColumnName("stockCategoryName");
            entity.Property(e => e.IsGstApplied).HasColumnName("isGstApplied");
            entity.Property(e => e.StockMediaUrl).HasColumnName("stockMediaUrl");
            entity.Property(e => e.DateAddedUtc)
                  .HasColumnName("dateAddedUTC")
                  .HasDefaultValueSql("GETUTCDATE()")
                  .ValueGeneratedOnAdd();

            entity.HasOne(e => e.Supplier)
                  .WithMany(s => s.StockItems)
                  .HasForeignKey(e => e.SupplierGUID)
                  .HasConstraintName("FK_StockItem_Supplier");
        });

        // StockItemUnitsOfMeasurementAndPrice: one StockItem has many tiers (cascade delete)
        modelBuilder.Entity<StockItemUnitsOfMeasurementAndPrice>(entity =>
        {
            entity.ToTable("StockItemUnitsOfMeasurementAndPrice");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.SupplierGUID).HasColumnName("supplierGUID");
            entity.Property(e => e.StockCode).HasColumnName("stockCode");
            entity.Property(e => e.SupplierCost).HasColumnName("supplierCost");
            entity.Property(e => e.StockUnit).HasColumnName("stockUnit");
            entity.Property(e => e.UnitOfMeasurementName).HasColumnName("unitOfMeasurementName");
            entity.Property(e => e.SortOrder).HasColumnName("sortOrder");

            entity.HasOne(e => e.StockItem)
                  .WithMany(s => s.PricingTiers)
                  .HasForeignKey(e => new { e.SupplierGUID, e.StockCode })
                  .HasConstraintName("FK_StockItemUnitsOfMeasurementAndPrice_StockItem")
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // AppUser
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("AppUser");
            entity.HasKey(e => e.AppUserId);
            entity.HasIndex(e => e.UserEmail).IsUnique();
        });

        // AppUserSupplier (intersection table — many-to-many between AppUser and Supplier)
        modelBuilder.Entity<AppUserSupplier>(entity =>
        {
            entity.ToTable("AppUserSupplier");
            entity.HasKey(e => new { e.AppUserId, e.SupplierGUID });

            entity.HasOne(e => e.AppUser)
                  .WithMany(u => u.AppUserSuppliers)
                  .HasForeignKey(e => e.AppUserId)
                  .HasConstraintName("FK_AppUserSupplier_AppUser");

            entity.HasOne(e => e.Supplier)
                  .WithMany()
                  .HasForeignKey(e => e.SupplierGUID)
                  .HasConstraintName("FK_AppUserSupplier_Supplier");
        });

        // LoginHistory
        modelBuilder.Entity<LoginHistory>(entity =>
        {
            entity.ToTable("LoginHistory");
            entity.HasKey(e => e.LoginHistoryId);
        });

        // AppPermission
        modelBuilder.Entity<AppPermission>(entity =>
        {
            entity.ToTable("AppPermission");
            entity.HasKey(e => e.AppPermissionId);
            entity.HasIndex(e => e.Description).IsUnique();
            entity.Property(e => e.Description).HasMaxLength(50);
        });

        // AppUserPermission (intersection table)
        modelBuilder.Entity<AppUserPermission>(entity =>
        {
            entity.ToTable("AppUserPermission");
            entity.HasKey(e => new { e.AppUserId, e.AppPermissionId });

            entity.HasOne(e => e.AppUser)
                  .WithMany(u => u.AppUserPermissions)
                  .HasForeignKey(e => e.AppUserId)
                  .HasConstraintName("FK_AppUserPermission_AppUser");

            entity.HasOne(e => e.AppPermission)
                  .WithMany(p => p.AppUserPermissions)
                  .HasForeignKey(e => e.AppPermissionId)
                  .HasConstraintName("FK_AppUserPermission_AppPermission");
        });
    }
}
