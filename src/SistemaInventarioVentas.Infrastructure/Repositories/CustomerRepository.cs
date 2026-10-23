using Microsoft.EntityFrameworkCore;
using Npgsql;
using SistemaInventarioVentas.Application.Exceptions;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Infrastructure.Persistence;

namespace SistemaInventarioVentas.Infrastructure.Repositories;

public sealed class CustomerRepository(CatalogDbContext dbContext) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        dbContext.Customers.SingleOrDefaultAsync(
            customer => customer.Id == id,
            cancellationToken);

    public async Task<IReadOnlyList<Customer>> ListAsync(
        CustomerQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<Customer> customers = dbContext.Customers;

        if (query.IsActive is bool isActive)
        {
            customers = customers.Where(customer => customer.IsActive == isActive);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.Trim();
            customers = customers.Where(customer =>
                EF.Functions.ILike(customer.Name, $"%{searchTerm}%") ||
                (customer.IdentificationNumber != null && EF.Functions.ILike(customer.IdentificationNumber, $"%{searchTerm}%")) ||
                (customer.Email != null && EF.Functions.ILike(EF.Property<string>(customer, "EmailKey"), $"%{searchTerm}%")) ||
                (customer.Phone != null && EF.Functions.ILike(customer.Phone, $"%{searchTerm}%")));
        }

        return await customers.OrderBy(customer => customer.Name).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByIdentificationNumberAsync(
        string normalizedIdentificationNumber,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedIdentificationNumber);

        var key = normalizedIdentificationNumber.Trim().ToUpperInvariant();
        var query = excludingId is Guid id
            ? dbContext.Customers.Where(customer => customer.Id != id)
            : dbContext.Customers;

        return query.AnyAsync(
            customer => EF.Property<string>(customer, "IdentificationNumberKey") == key,
            cancellationToken);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customer);

        try
        {
            await dbContext.Customers.AddAsync(customer, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsIdentificationUniqueConstraintViolation(exception))
        {
            throw new DuplicateCustomerIdentificationException(
                "A customer with this identification number already exists.",
                exception);
        }
    }

    public async Task SaveAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customer);

        try
        {
            if (dbContext.Entry(customer).State == EntityState.Detached)
            {
                dbContext.Customers.Update(customer);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsIdentificationUniqueConstraintViolation(exception))
        {
            throw new DuplicateCustomerIdentificationException(
                "A customer with this identification number already exists.",
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
                postgresException.ConstraintName.Contains("ux_customers_identification_number", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
