using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TenantCore.Application.Interfaces;
using TenantCore.Domain.Common;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<Infrastructure.Identity.ApplicationUser, Infrastructure.Identity.ApplicationRole, Guid>
{
    private readonly ITenantProvider? _tenantProvider;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ITenantProvider? tenantProvider = null,
        IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _tenantProvider = tenantProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<TenantSubscription> TenantSubscriptions { get; set; }
    public DbSet<Domain.Entities.UserTenant> UserTenants { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Tenant Configuration
        builder.Entity<Tenant>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
            entity.Property(t => t.Domain).IsRequired().HasMaxLength(100);
            entity.HasIndex(t => t.Domain).IsUnique();

            // API Key Configuration (Enterprise Security)
            entity.Property(t => t.ApiKeyHash).IsRequired().HasMaxLength(64);
            entity.Property(t => t.ApiKeyPrefix).IsRequired().HasMaxLength(20);
            entity.HasIndex(t => t.ApiKeyHash).IsUnique();
            entity.HasIndex(t => t.ApiKeyPrefix);
            entity.Property(t => t.ApiRateLimitPerHour).HasDefaultValue(1000);

            entity.HasQueryFilter(t => !t.IsDeleted);

            entity.HasMany(t => t.Subscriptions)
                .WithOne(s => s.Tenant)
                .HasForeignKey(s => s.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ApplicationUser Configuration
        builder.Entity<Infrastructure.Identity.ApplicationUser>(entity =>
        {
            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(50);

            // Foreign key relationship to Tenant
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            entity.HasIndex(u => u.TenantId);

            // Global Query Filter for Tenant Isolation
            entity.HasQueryFilter(u =>
                !u.IsDeleted &&
                (_tenantProvider == null || _tenantProvider.IsSuperAdmin || u.TenantId == _tenantProvider.CurrentTenantId));
        });

        // SubscriptionPlan Configuration
        builder.Entity<SubscriptionPlan>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.Property(p => p.PricePerMonth).HasColumnType("decimal(18,2)");
        });

        // TenantSubscription Configuration
        builder.Entity<TenantSubscription>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Status).IsRequired();

            entity.HasOne(s => s.Tenant)
                .WithMany(t => t.Subscriptions)
                .HasForeignKey(s => s.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Plan)
                .WithMany(p => p.TenantSubscriptions)
                .HasForeignKey(s => s.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            // Global Query Filter for Tenant Isolation
            entity.HasQueryFilter(s =>
                _tenantProvider == null || _tenantProvider.IsSuperAdmin || s.TenantId == _tenantProvider.CurrentTenantId);
        });

        // UserTenant Configuration (Many-to-Many)
        builder.Entity<Domain.Entities.UserTenant>(entity =>
        {
            entity.HasKey(ut => ut.Id);

            // Composite unique index to prevent duplicate user-tenant mappings
            entity.HasIndex(ut => new { ut.UserId, ut.TenantId }).IsUnique();

            entity.Property(ut => ut.Role).IsRequired().HasMaxLength(50);

            // Configure relationship to Tenant
            entity.HasOne(ut => ut.Tenant)
                .WithMany(t => t.UserTenants)
                .HasForeignKey(ut => ut.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure relationship to ApplicationUser
            entity.HasOne<Infrastructure.Identity.ApplicationUser>()
                .WithMany(u => u.UserTenants)
                .HasForeignKey(ut => ut.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Global Query Filter for Tenant Isolation
            entity.HasQueryFilter(ut =>
                _tenantProvider == null || _tenantProvider.IsSuperAdmin || ut.TenantId == _tenantProvider.CurrentTenantId);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Automatic Audit Tracking
        var entries = ChangeTracker.Entries<IAuditableEntity>();
        var currentUser = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "System";

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.CreatedBy = currentUser;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedBy = currentUser;
            }
        }

        // Soft Delete Tracking
        var softDeleteEntries = ChangeTracker.Entries<ISoftDelete>()
            .Where(e => e.State == EntityState.Deleted);

        foreach (var entry in softDeleteEntries)
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = DateTime.UtcNow;
            entry.Entity.DeletedBy = currentUser;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
