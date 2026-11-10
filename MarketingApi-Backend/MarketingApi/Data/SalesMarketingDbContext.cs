using MarketingApi.Enums;
using MarketingApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketingApi.Data;

public partial class SalesMarketingDbContext : DbContext
{
    public SalesMarketingDbContext(DbContextOptions<SalesMarketingDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<MarketingTeam> MarketingTeams { get; set; }
    public virtual DbSet<MarketingTeamDetail> MarketingTeamDetails { get; set; }
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<Salesman> Salesmen { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<SalesmanDeactivationRequest> SalesmanDeactivationRequests { get; set; }
    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MarketingTeam>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("marketingteam_pkey");
            entity.ToTable("marketingteam");

            entity.HasIndex(e => e.TeamCode, "marketingteam_teamcode_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TeamCode).HasMaxLength(20).HasColumnName("teamcode");
            entity.Property(e => e.TeamName).HasMaxLength(100).HasColumnName("teamname");
            entity.Property(e => e.TeamLeaderId).HasColumnName("teamleaderid");
            entity.Property(e => e.Description).HasMaxLength(500).HasColumnName("description");
            entity.Property(e => e.Status).HasDefaultValue((short)RecordStatus.Active).HasColumnName("status");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("now()").HasColumnName("createddate");
            entity.Property(e => e.CreatedBy).HasMaxLength(50).HasColumnName("createdby");
            entity.Property(e => e.ModifiedDate).HasColumnName("modifieddate");
            entity.Property(e => e.ModifiedBy).HasMaxLength(50).HasColumnName("modifiedby");

            entity.HasOne(d => d.TeamLeader).WithMany(p => p.MarketingTeams)
                .HasForeignKey(d => d.TeamLeaderId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_marketingteam_teamleader");
        });

        modelBuilder.Entity<MarketingTeamDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("marketingteamdetail_pkey");
            entity.ToTable("marketingteamdetail");

            entity.HasIndex(e => new { e.MarketingTeamId, e.SalesmanId }, "uq_marketingteamdetail_team_salesman").IsUnique();

            // Enforce Rule R8 at DB level: a salesman can have at most one Active detail row
            entity.HasIndex(e => e.SalesmanId, "uq_marketingteamdetail_active_salesman")
                .IsUnique()
                .HasFilter("status = 1");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MarketingTeamId).HasColumnName("marketingteamid");
            entity.Property(e => e.SalesmanId).HasColumnName("salesmanid");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.TargetAmount).HasPrecision(18, 2).HasColumnName("targetamount");
            entity.Property(e => e.Status).HasDefaultValue((short)RecordStatus.Active).HasColumnName("status");

            entity.HasOne(d => d.MarketingTeam).WithMany(p => p.MarketingTeamDetails)
                .HasForeignKey(d => d.MarketingTeamId)
                .HasConstraintName("fk_teamdetail_team");

            entity.HasOne(d => d.Salesman).WithMany(p => p.MarketingTeamDetails)
                .HasForeignKey(d => d.SalesmanId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_teamdetail_salesman");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roles_pkey");
            entity.ToTable("roles");

            entity.HasIndex(e => e.RoleName, "roles_rolename_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoleName).HasMaxLength(50).HasColumnName("rolename");
        });

        modelBuilder.Entity<Salesman>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("salesman_pkey");
            entity.ToTable("salesman");

            entity.HasIndex(e => e.SalesmanCode, "salesman_salesmancode_key").IsUnique();
            entity.HasIndex(e => e.MobileNo, "uq_salesman_mobileno")
                .IsUnique()
                .HasFilter("mobileno IS NOT NULL");
            entity.HasIndex(e => e.Email, "uq_salesman_email")
                .IsUnique()
                .HasFilter("email IS NOT NULL");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SalesmanCode).HasMaxLength(20).HasColumnName("salesmancode");
            entity.Property(e => e.SalesmanName).HasMaxLength(100).HasColumnName("salesmanname");
            entity.Property(e => e.MobileNo).HasMaxLength(20).HasColumnName("mobileno");
            entity.Property(e => e.Email).HasMaxLength(100).HasColumnName("email");
            entity.Property(e => e.JoiningDate).HasColumnName("joiningdate");
            entity.Property(e => e.Status).HasDefaultValue((short)RecordStatus.Active).HasColumnName("status");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("now()").HasColumnName("createddate");
            entity.Property(e => e.CreatedBy).HasMaxLength(50).HasColumnName("createdby");
            entity.Property(e => e.ModifiedDate).HasColumnName("modifieddate");
            entity.Property(e => e.ModifiedBy).HasMaxLength(50).HasColumnName("modifiedby");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");
            entity.ToTable("users");

            // Display names need not be unique (M13); only username is unique
            entity.HasIndex(e => e.Username, "users_username_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
            entity.Property(e => e.PasswordHash).HasMaxLength(255).HasColumnName("passwordhash");
            entity.Property(e => e.RoleId).HasColumnName("roleid");
            entity.Property(e => e.Username).HasMaxLength(50).HasColumnName("username");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_users_roles");
        });

        modelBuilder.Entity<SalesmanDeactivationRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("salesman_deactivation_request_pkey");
            entity.ToTable("salesman_deactivation_request");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SalesmanId).HasColumnName("salesmanid");
            entity.Property(e => e.SalesmanCode).HasMaxLength(20).HasColumnName("salesmancode");
            entity.Property(e => e.SalesmanName).HasMaxLength(100).HasColumnName("salesmanname");
            entity.Property(e => e.TeamId).HasColumnName("teamid");
            entity.Property(e => e.TeamCode).HasMaxLength(20).HasColumnName("teamcode");
            entity.Property(e => e.TeamName).HasMaxLength(100).HasColumnName("teamname");
            entity.Property(e => e.RoleInTeam).HasMaxLength(50).HasColumnName("roleinteam");
            entity.Property(e => e.Reason).HasMaxLength(500).HasColumnName("reason");
            entity.Property(e => e.RequestedBy).HasMaxLength(50).HasColumnName("requestedby");
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue(nameof(DeactivationRequestStatus.Pending)).HasColumnName("status");
            entity.Property(e => e.RequestedDate).HasDefaultValueSql("now()").HasColumnName("requesteddate");
            entity.Property(e => e.ProcessedDate).HasColumnName("processeddate");
            entity.Property(e => e.ProcessedBy).HasMaxLength(50).HasColumnName("processedby");
            entity.Property(e => e.Notes).HasMaxLength(500).HasColumnName("notes");

            // PostgreSQL xmin optimistic concurrency token (H4)
            entity.Property(e => e.Version).IsRowVersion();

            entity.HasOne(d => d.Salesman).WithMany()
                .HasForeignKey(d => d.SalesmanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Team).WithMany()
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("audit_log_pkey");
            entity.ToTable("audit_log");

            entity.HasIndex(e => new { e.EntityName, e.EntityId }, "ix_audit_log_entity");
            entity.HasIndex(e => e.ChangedAt, "ix_audit_log_changed_at");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EntityName).HasMaxLength(100).HasColumnName("entity_name");
            entity.Property(e => e.EntityId).HasMaxLength(50).HasColumnName("entity_id");
            entity.Property(e => e.Action).HasMaxLength(20).HasColumnName("action");
            entity.Property(e => e.OldValues).HasColumnType("text").HasColumnName("old_values");
            entity.Property(e => e.NewValues).HasColumnType("text").HasColumnName("new_values");
            entity.Property(e => e.ChangedBy).HasMaxLength(50).HasColumnName("changed_by");
            entity.Property(e => e.ChangedAt).HasDefaultValueSql("now()").HasColumnName("changed_at");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
