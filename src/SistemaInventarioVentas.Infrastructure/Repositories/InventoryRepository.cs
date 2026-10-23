using Microsoft.EntityFrameworkCore;
using Npgsql;
using SistemaInventarioVentas.Application.Exceptions;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;
using SistemaInventarioVentas.Infrastructure.Persistence;

namespace SistemaInventarioVentas.Infrastructure.Repositories;

public sealed class InventoryRepository(CatalogDbContext dbContext) : IInventoryRepository
{
    public Task<Inventory?> GetByProductIdentifierAsync(
        ProductIdentifier productIdentifier,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(productIdentifier);

        return dbContext.Inventories
            .Include(inventory => inventory.Movements)
            .SingleOrDefaultAsync(
                inventory => inventory.ProductIdentifier == productIdentifier,
                cancellationToken);
    }

    public Task AddAsync(Inventory inventory, CancellationToken cancellationToken = default) =>
        AddAsync(inventory, auditEvent: null, cancellationToken);

    public async Task AddAsync(
        Inventory inventory,
        AuditEvent? auditEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        try
        {
            await dbContext.Inventories.AddAsync(inventory, cancellationToken);
            if (auditEvent is not null)
            {
                await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            if (auditEvent is not null)
            {
                dbContext.Entry(auditEvent).State = EntityState.Detached;
            }

            throw new ConcurrencyException(
                "An inventory record for this product was already initialized by a concurrent operation.",
                exception);
        }
        catch
        {
            if (auditEvent is not null)
            {
                dbContext.Entry(auditEvent).State = EntityState.Detached;
            }

            throw;
        }
    }

    public Task SaveAsync(Inventory inventory, CancellationToken cancellationToken = default) =>
        SaveAsync(inventory, auditEvent: null, cancellationToken);

    public async Task SaveAsync(
        Inventory inventory,
        AuditEvent? auditEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        if (dbContext.Entry(inventory).State == EntityState.Detached)
        {
            var currentVersion = await dbContext.Inventories
                .Where(i => i.Id == inventory.Id)
                .Select(i => EF.Property<uint>(i, "Version"))
                .SingleOrDefaultAsync(cancellationToken);

            dbContext.Inventories.Attach(inventory);
            dbContext.Entry(inventory).State = EntityState.Modified;
            if (currentVersion != 0)
            {
                dbContext.Entry(inventory).Property<uint>("Version").OriginalValue = currentVersion;
            }

            var existingIds = await dbContext.InventoryMovements
                .Where(m => EF.Property<Guid>(m, "InventoryId") == inventory.Id)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            var existingIdSet = existingIds.ToHashSet();

            foreach (var movement in inventory.Movements)
            {
                var entry = dbContext.Entry(movement);
                entry.State = existingIdSet.Contains(movement.Id)
                    ? EntityState.Unchanged
                    : EntityState.Added;
            }
        }

        if (auditEvent is not null)
        {
            await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            if (auditEvent is not null)
            {
                dbContext.Entry(auditEvent).State = EntityState.Detached;
            }

            throw new ConcurrencyException(
                "The inventory was modified by another operation. Please retry.",
                exception);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            if (auditEvent is not null)
            {
                dbContext.Entry(auditEvent).State = EntityState.Detached;
            }

            throw new ConcurrencyException(
                "A concurrent modification conflicted with existing inventory records.",
                exception);
        }
        catch
        {
            if (auditEvent is not null)
            {
                dbContext.Entry(auditEvent).State = EntityState.Detached;
            }

            throw;
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return true;
            }
        }

        return false;
    }
}
