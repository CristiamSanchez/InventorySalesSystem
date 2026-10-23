using Microsoft.EntityFrameworkCore;
using Npgsql;
using SistemaInventarioVentas.Application.Exceptions;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Infrastructure.Persistence;

namespace SistemaInventarioVentas.Infrastructure.Repositories;

public sealed class SaleRepository(CatalogDbContext dbContext) : ISaleRepository
{
    public Task<Sale?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Sales
            .Include(sale => sale.Details)
            .SingleOrDefaultAsync(sale => sale.Id == id, cancellationToken);
    }

    public Task AddAsync(
        Sale sale,
        CancellationToken cancellationToken = default) =>
        AddAsync(sale, auditEvent: null, cancellationToken);

    public async Task AddAsync(
        Sale sale,
        AuditEvent? auditEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sale);

        await dbContext.Sales.AddAsync(sale, cancellationToken);
        if (auditEvent is not null)
        {
            await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
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

    public Task SaveAsync(
        Sale sale,
        CancellationToken cancellationToken = default) =>
        SaveAsync(sale, auditEvent: null, cancellationToken);

    public async Task SaveAsync(
        Sale sale,
        AuditEvent? auditEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sale);

        if (dbContext.Entry(sale).State == EntityState.Detached)
        {
            var existingIds = await dbContext.SaleDetails
                .Where(detail => detail.SaleId == sale.Id)
                .Select(detail => detail.Id)
                .ToListAsync(cancellationToken);

            var existingIdSet = existingIds.ToHashSet();
            var currentIdSet = sale.Details.Select(detail => detail.Id).ToHashSet();
            var removedIds = existingIdSet.Except(currentIdSet).ToList();

            if (removedIds.Count > 0)
            {
                await dbContext.SaleDetails
                    .Where(detail => removedIds.Contains(detail.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            dbContext.Sales.Attach(sale);
            dbContext.Entry(sale).State = EntityState.Modified;

            foreach (var detail in sale.Details)
            {
                var entry = dbContext.Entry(detail);
                entry.State = existingIdSet.Contains(detail.Id)
                    ? EntityState.Modified
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
        catch
        {
            if (auditEvent is not null)
            {
                dbContext.Entry(auditEvent).State = EntityState.Detached;
            }

            throw;
        }
    }

    public Task ConfirmSaleAsync(
        Sale sale,
        IReadOnlyCollection<Inventory> inventories,
        CancellationToken cancellationToken = default) =>
        ConfirmSaleAsync(sale, inventories, auditEvent: null, cancellationToken);

    public async Task ConfirmSaleAsync(
        Sale sale,
        IReadOnlyCollection<Inventory> inventories,
        AuditEvent? auditEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sale);
        ArgumentNullException.ThrowIfNull(inventories);

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var currentStatus = await dbContext.Sales
                    .AsNoTracking()
                    .Where(s => s.Id == sale.Id)
                    .Select(s => (SaleStatus?)s.Status)
                    .SingleOrDefaultAsync(cancellationToken);

                if (currentStatus != SaleStatus.Draft)
                {
                    throw new ConcurrencyException(
                        "The sale has already been confirmed or modified by another operation.");
                }

                if (dbContext.Entry(sale).State == EntityState.Detached)
                {
                    dbContext.Sales.Attach(sale);
                    dbContext.Entry(sale).State = EntityState.Modified;
                }

                foreach (var inventory in inventories)
                {
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

                        var existingMovementIds = await dbContext.InventoryMovements
                            .Where(m => EF.Property<Guid>(m, "InventoryId") == inventory.Id)
                            .Select(m => m.Id)
                            .ToListAsync(cancellationToken);

                        var existingMovementIdSet = existingMovementIds.ToHashSet();

                        foreach (var movement in inventory.Movements)
                        {
                            var entry = dbContext.Entry(movement);
                            entry.State = existingMovementIdSet.Contains(movement.Id)
                                ? EntityState.Unchanged
                                : EntityState.Added;
                        }
                    }
                }

                if (auditEvent is not null)
                {
                    await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                if (auditEvent is not null)
                {
                    dbContext.Entry(auditEvent).State = EntityState.Detached;
                }

                throw new ConcurrencyException(
                    "The inventory or sale was modified by another operation. Please retry.",
                    exception);
            }
            catch (DbUpdateException exception) when (IsCheckConstraintViolation(exception) || IsUniqueConstraintViolation(exception))
            {
                await transaction.RollbackAsync(CancellationToken.None);
                if (auditEvent is not null)
                {
                    dbContext.Entry(auditEvent).State = EntityState.Detached;
                }

                throw new ConcurrencyException(
                    "A concurrency conflict occurred while confirming the sale.",
                    exception);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                if (auditEvent is not null)
                {
                    dbContext.Entry(auditEvent).State = EntityState.Detached;
                }

                throw;
            }
        });
    }

    private static bool IsCheckConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.CheckViolation })
            {
                return true;
            }
        }

        return false;
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
