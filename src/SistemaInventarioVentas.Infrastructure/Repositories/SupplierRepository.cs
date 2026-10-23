using Microsoft.EntityFrameworkCore;
using Npgsql;
using SistemaInventarioVentas.Application.Exceptions;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Infrastructure.Persistence;

namespace SistemaInventarioVentas.Infrastructure.Repositories;

public sealed class SupplierRepository(CatalogDbContext dbContext) : ISupplierRepository
{
    public Task<Supplier?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        dbContext.Suppliers.SingleOrDefaultAsync(
            supplier => supplier.Id == id,
            cancellationToken);

    public async Task<IReadOnlyList<Supplier>> ListAsync(
        SupplierQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<Supplier> suppliers = dbContext.Suppliers;

        if (query.IsActive is bool isActive)
        {
            suppliers = suppliers.Where(supplier => supplier.IsActive == isActive);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.Trim();
            suppliers = suppliers.Where(supplier =>
                EF.Functions.ILike(supplier.Name, $"%{searchTerm}%") ||
                (supplier.ContactName != null && EF.Functions.ILike(supplier.ContactName, $"%{searchTerm}%")) ||
                (supplier.IdentificationNumber != null && EF.Functions.ILike(supplier.IdentificationNumber, $"%{searchTerm}%")) ||
                (supplier.Email != null && EF.Functions.ILike(EF.Property<string>(supplier, "EmailKey"), $"%{searchTerm}%")) ||
                (supplier.Phone != null && EF.Functions.ILike(supplier.Phone, $"%{searchTerm}%")));
        }

        return await suppliers.OrderBy(supplier => supplier.Name).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByIdentificationNumberAsync(
        string normalizedIdentificationNumber,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedIdentificationNumber);

        var key = normalizedIdentificationNumber.Trim().ToUpperInvariant();
        var query = excludingId is Guid id
            ? dbContext.Suppliers.Where(supplier => supplier.Id != id)
            : dbContext.Suppliers;

        return query.AnyAsync(
            supplier => EF.Property<string>(supplier, "IdentificationNumberKey") == key,
            cancellationToken);
    }

    public async Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(supplier);

        try
        {
            await dbContext.Suppliers.AddAsync(supplier, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsIdentificationUniqueConstraintViolation(exception))
        {
            throw new DuplicateSupplierIdentificationException(
                "A supplier with this identification number already exists.",
                exception);
        }
    }

    public async Task SaveAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(supplier);

        try
        {
            if (dbContext.Entry(supplier).State == EntityState.Detached)
            {
                dbContext.Suppliers.Update(supplier);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsIdentificationUniqueConstraintViolation(exception))
        {
            throw new DuplicateSupplierIdentificationException(
                "A supplier with this identification number already exists.",
                exception);
        }
    }

    private static bool IsIdentificationUniqueConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgresException &&
                postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
                postgresException.ConstraintName != null &&
                postgresException.ConstraintName.Contains("ux_suppliers_identification_number", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
