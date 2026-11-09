using System;
using System.Collections.Generic;
using MarketingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketingApi.Data;

public partial class SalesMarketingDbContext : DbContext
{
    public SalesMarketingDbContext(DbContextOptions<SalesMarketingDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Marketingteam> Marketingteams { get; set; }

    public virtual DbSet<Marketingteamdetail> Marketingteamdetails { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Salesman> Salesmen { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<SalesmanDeactivationRequest> SalesmanDeactivationRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Marketingteam>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("marketingteam_pkey");

            entity.ToTable("marketingteam");

            entity.HasIndex(e => e.Teamcode, "marketingteam_teamcode_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Createddate)
                .HasDefaultValueSql("now()")
                .HasColumnName("createddate");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.Modifieddate).HasColumnName("modifieddate");
            entity.Property(e => e.Status)
                .HasDefaultValue((short)1)
                .HasColumnName("status");
            entity.Property(e => e.Teamcode)
                .HasMaxLength(20)
                .HasColumnName("teamcode");
            entity.Property(e => e.Teamleaderid).HasColumnName("teamleaderid");
            entity.Property(e => e.Teamname)
                .HasMaxLength(100)
                .HasColumnName("teamname");

            entity.HasOne(d => d.Teamleader).WithMany(p => p.Marketingteams)
                .HasForeignKey(d => d.Teamleaderid)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_marketingteam_teamleader");
        });

        modelBuilder.Entity<Marketingteamdetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("marketingteamdetail_pkey");

            entity.ToTable("marketingteamdetail");

            entity.HasIndex(e => new { e.Marketingteamid, e.Salesmanid }, "uq_marketingteamdetail_team_salesman").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Marketingteamid).HasColumnName("marketingteamid");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.Salesmanid).HasColumnName("salesmanid");
            entity.Property(e => e.Status)
                .HasDefaultValue((short)1)
                .HasColumnName("status");
            entity.Property(e => e.Targetamount)
                .HasPrecision(18, 2)
                .HasColumnName("targetamount");

            entity.HasOne(d => d.Marketingteam).WithMany(p => p.Marketingteamdetails)
                .HasForeignKey(d => d.Marketingteamid)
                .HasConstraintName("fk_teamdetail_team");

            entity.HasOne(d => d.Salesman).WithMany(p => p.Marketingteamdetails)
                .HasForeignKey(d => d.Salesmanid)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_teamdetail_salesman");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roles_pkey");

            entity.ToTable("roles");

            entity.HasIndex(e => e.Rolename, "roles_rolename_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Rolename)
                .HasMaxLength(50)
                .HasColumnName("rolename");
        });

        modelBuilder.Entity<Salesman>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("salesman_pkey");

            entity.ToTable("salesman");

            entity.HasIndex(e => e.Salesmancode, "salesman_salesmancode_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Createddate)
                .HasDefaultValueSql("now()")
                .HasColumnName("createddate");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Joiningdate).HasColumnName("joiningdate");
            entity.Property(e => e.Mobileno)
                .HasMaxLength(20)
                .HasColumnName("mobileno");
            entity.Property(e => e.Modifieddate).HasColumnName("modifieddate");
            entity.Property(e => e.Salesmancode)
                .HasMaxLength(20)
                .HasColumnName("salesmancode");
            entity.Property(e => e.Salesmanname)
                .HasMaxLength(100)
                .HasColumnName("salesmanname");
            entity.Property(e => e.Status)
                .HasDefaultValue((short)1)
                .HasColumnName("status");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Name, "users_name_key").IsUnique();

            entity.HasIndex(e => e.Username, "users_username_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Passwordhash)
                .HasMaxLength(255)
                .HasColumnName("passwordhash");
            entity.Property(e => e.Roleid).HasColumnName("roleid");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.Roleid)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_users_roles");
        });

        modelBuilder.Entity<SalesmanDeactivationRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("salesman_deactivation_request_pkey");

            entity.ToTable("salesman_deactivation_request");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Salesmanid).HasColumnName("salesmanid");
            entity.Property(e => e.Salesmancode).HasMaxLength(20).HasColumnName("salesmancode");
            entity.Property(e => e.Salesmanname).HasMaxLength(100).HasColumnName("salesmanname");
            entity.Property(e => e.Teamid).HasColumnName("teamid");
            entity.Property(e => e.Teamcode).HasMaxLength(20).HasColumnName("teamcode");
            entity.Property(e => e.Teamname).HasMaxLength(100).HasColumnName("teamname");
            entity.Property(e => e.Roleinteam).HasMaxLength(50).HasColumnName("roleinteam");
            entity.Property(e => e.Reason).HasMaxLength(500).HasColumnName("reason");
            entity.Property(e => e.Requestedby).HasMaxLength(50).HasDefaultValue("superviser").HasColumnName("requestedby");
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Pending").HasColumnName("status");
            entity.Property(e => e.Requesteddate).HasDefaultValueSql("now()").HasColumnName("requesteddate");
            entity.Property(e => e.Processeddate).HasColumnName("processeddate");
            entity.Property(e => e.Processedby).HasMaxLength(50).HasColumnName("processedby");
            entity.Property(e => e.Notes).HasMaxLength(500).HasColumnName("notes");

            entity.HasOne(d => d.Salesman).WithMany()
                .HasForeignKey(d => d.Salesmanid)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Team).WithMany()
                .HasForeignKey(d => d.Teamid)
                .OnDelete(DeleteBehavior.SetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
