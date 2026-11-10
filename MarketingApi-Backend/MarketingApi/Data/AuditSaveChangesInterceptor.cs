using System.Runtime.CompilerServices;
using System.Text.Json;
using MarketingApi.Common;
using MarketingApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MarketingApi.Data;

/// <summary>
/// Automatically populates audit columns (CreatedBy/ModifiedBy/timestamps) and writes field-level audit logs.
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;
    private readonly ConditionalWeakTable<DbContext, List<PendingAuditEntry>> _pendingAddedAudits = new();
    private bool _isWritingAuditLogs;

    public AuditSaveChangesInterceptor(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        PrepareAuditChanges(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        PrepareAuditChanges(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        FlushPendingAddedAudits(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await FlushPendingAddedAuditsAsync(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void PrepareAuditChanges(DbContext? context)
    {
        if (context == null || _isWritingAuditLogs)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var username = _currentUser.Username ?? "system";
        var pendingAdded = new List<PendingAuditEntry>();

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is AuditLog || entry.Entity is User || entry.Entity is Role)
            {
                continue;
            }

            if (entry.Entity is IAuditableEntity auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    if (auditable.CreatedDate == default)
                    {
                        auditable.CreatedDate = now;
                    }
                    auditable.CreatedBy ??= username;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditable.ModifiedDate = now;
                    auditable.ModifiedBy = username;
                }
            }

            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                pendingAdded.Add(new PendingAuditEntry(entry, username, now));
                continue;
            }

            var auditLog = BuildAuditLog(entry, username, now);
            if (auditLog != null)
            {
                context.Set<AuditLog>().Add(auditLog);
            }
        }

        if (pendingAdded.Count > 0)
        {
            _pendingAddedAudits.Remove(context);
            _pendingAddedAudits.Add(context, pendingAdded);
        }
    }

    private void FlushPendingAddedAudits(DbContext? context)
    {
        if (context == null || _isWritingAuditLogs || !_pendingAddedAudits.TryGetValue(context, out var pending))
        {
            return;
        }

        _pendingAddedAudits.Remove(context);
        var logs = pending
            .Select(p => BuildAddedAuditLog(p.Entry, p.ChangedBy, p.ChangedAt))
            .Where(l => l != null)
            .Cast<AuditLog>()
            .ToList();

        if (logs.Count == 0)
        {
            return;
        }

        _isWritingAuditLogs = true;
        try
        {
            context.Set<AuditLog>().AddRange(logs);
            context.SaveChanges();
        }
        finally
        {
            _isWritingAuditLogs = false;
        }
    }

    private async Task FlushPendingAddedAuditsAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context == null || _isWritingAuditLogs || !_pendingAddedAudits.TryGetValue(context, out var pending))
        {
            return;
        }

        _pendingAddedAudits.Remove(context);
        var logs = pending
            .Select(p => BuildAddedAuditLog(p.Entry, p.ChangedBy, p.ChangedAt))
            .Where(l => l != null)
            .Cast<AuditLog>()
            .ToList();

        if (logs.Count == 0)
        {
            return;
        }

        _isWritingAuditLogs = true;
        try
        {
            context.Set<AuditLog>().AddRange(logs);
            await context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _isWritingAuditLogs = false;
        }
    }

    private static AuditLog? BuildAddedAuditLog(EntityEntry entry, string username, DateTime now)
    {
        var newValues = new Dictionary<string, object?>();
        foreach (var prop in entry.Properties)
        {
            if (prop.Metadata.IsShadowProperty() || prop.Metadata.IsConcurrencyToken)
            {
                continue;
            }
            newValues[prop.Metadata.Name] = prop.CurrentValue;
        }

        return new AuditLog
        {
            EntityName = entry.Metadata.ClrType.Name,
            EntityId = ResolvePrimaryKey(entry),
            Action = "Insert",
            OldValues = null,
            NewValues = JsonSerializer.Serialize(newValues),
            ChangedBy = username,
            ChangedAt = now
        };
    }

    private static AuditLog? BuildAuditLog(EntityEntry entry, string username, DateTime now)
    {
        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();

        foreach (var prop in entry.Properties)
        {
            if (prop.Metadata.IsShadowProperty() || prop.Metadata.IsConcurrencyToken)
            {
                continue;
            }

            if (entry.State == EntityState.Deleted)
            {
                oldValues[prop.Metadata.Name] = prop.OriginalValue;
            }
            else if (entry.State == EntityState.Modified && prop.IsModified && !Equals(prop.OriginalValue, prop.CurrentValue))
            {
                oldValues[prop.Metadata.Name] = prop.OriginalValue;
                newValues[prop.Metadata.Name] = prop.CurrentValue;
            }
        }

        if (entry.State == EntityState.Modified && newValues.Count == 0)
        {
            return null;
        }

        return new AuditLog
        {
            EntityName = entry.Metadata.ClrType.Name,
            EntityId = ResolvePrimaryKey(entry),
            Action = entry.State == EntityState.Deleted ? "Delete" : "Update",
            OldValues = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null,
            ChangedBy = username,
            ChangedAt = now
        };
    }

    private static string ResolvePrimaryKey(EntityEntry entry)
    {
        var pk = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        return pk?.CurrentValue?.ToString() ?? string.Empty;
    }

    private sealed record PendingAuditEntry(EntityEntry Entry, string ChangedBy, DateTime ChangedAt);
}
