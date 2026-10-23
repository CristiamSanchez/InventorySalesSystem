using SistemaInventarioVentas.Application.Common;
using SistemaInventarioVentas.Application.Exceptions;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Application.UseCases;

public sealed class CustomerUseCases(ICustomerRepository customers)
{
    public async Task<Result<Customer>> CreateAsync(
        string name,
        string? identificationNumber = null,
        string? email = null,
        string? phone = null,
        string? address = null,
        DateTimeOffset? createdAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        EmailAddress? emailAddress = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            try
            {
                emailAddress = new EmailAddress(email);
            }
            catch (ArgumentException exception)
            {
                return Result<Customer>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
            }
        }

        string? normalizedIdentification = null;
        if (!string.IsNullOrWhiteSpace(identificationNumber))
        {
            var trimmed = identificationNumber.Trim().ToUpperInvariant();
            if (trimmed.Length > 50)
            {
                return Result<Customer>.Failure(
                    ApplicationErrorCode.InvalidInput,
                    "Identification number cannot exceed 50 characters.");
            }

            if (await customers.ExistsByIdentificationNumberAsync(trimmed, cancellationToken: cancellationToken))
            {
                return Result<Customer>.Failure(
                    ApplicationErrorCode.CustomerIdentificationConflict,
                    "A customer with this identification number already exists.");
            }

            normalizedIdentification = trimmed;
        }

        Customer customer;
        try
        {
            customer = new Customer(
                name: name,
                identificationNumber: normalizedIdentification,
                email: emailAddress,
                phone: phone,
                address: address,
                createdAtUtc: createdAtUtc);
        }
        catch (ArgumentException exception)
        {
            return Result<Customer>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }

        try
        {
            await customers.AddAsync(customer, cancellationToken);
        }
        catch (DuplicateCustomerIdentificationException exception)
        {
            return Result<Customer>.Failure(
                ApplicationErrorCode.CustomerIdentificationConflict,
                exception.Message);
        }

        return Result<Customer>.Success(customer);
    }

    public async Task<Result<Customer>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<Customer>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Customer ID cannot be empty.");
        }

        var customer = await customers.GetByIdAsync(id, cancellationToken);
        return customer is null
            ? Result<Customer>.Failure(ApplicationErrorCode.NotFound, "Customer was not found.")
            : Result<Customer>.Success(customer);
    }

    public async Task<Result<IReadOnlyList<Customer>>> ListAsync(
        CustomerQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveQuery = query ?? new CustomerQuery();
        var normalizedQuery = effectiveQuery with { SearchTerm = effectiveQuery.SearchTerm?.Trim() };
        var result = await customers.ListAsync(normalizedQuery, cancellationToken);
        return Result<IReadOnlyList<Customer>>.Success(result);
    }

    public async Task<Result<Customer>> UpdateDetailsAsync(
        Guid id,
        string name,
        string? identificationNumber = null,
        string? email = null,
        string? phone = null,
        string? address = null,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<Customer>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Customer ID cannot be empty.");
        }

        var customer = await customers.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return Result<Customer>.Failure(
                ApplicationErrorCode.NotFound,
                "Customer was not found.");
        }

        EmailAddress? emailAddress = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            try
            {
                emailAddress = new EmailAddress(email);
            }
            catch (ArgumentException exception)
            {
                return Result<Customer>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
            }
        }

        string? normalizedIdentification = null;
        if (!string.IsNullOrWhiteSpace(identificationNumber))
        {
            var trimmed = identificationNumber.Trim().ToUpperInvariant();
            if (trimmed.Length > 50)
            {
                return Result<Customer>.Failure(
                    ApplicationErrorCode.InvalidInput,
                    "Identification number cannot exceed 50 characters.");
            }

            if (await customers.ExistsByIdentificationNumberAsync(trimmed, excludingId: id, cancellationToken: cancellationToken))
            {
                return Result<Customer>.Failure(
                    ApplicationErrorCode.CustomerIdentificationConflict,
                    "A customer with this identification number already exists.");
            }

            normalizedIdentification = trimmed;
        }

        try
        {
            customer.UpdateDetails(
                name: name,
                identificationNumber: normalizedIdentification,
                email: emailAddress,
                phone: phone,
                address: address);
        }
        catch (ArgumentException exception)
        {
            return Result<Customer>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }

        try
        {
            await customers.SaveAsync(customer, cancellationToken);
        }
        catch (DuplicateCustomerIdentificationException exception)
        {
            return Result<Customer>.Failure(
                ApplicationErrorCode.CustomerIdentificationConflict,
                exception.Message);
        }

        return Result<Customer>.Success(customer);
    }

    public async Task<Result<Customer>> ActivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<Customer>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Customer ID cannot be empty.");
        }

        var customer = await customers.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return Result<Customer>.Failure(
                ApplicationErrorCode.NotFound,
                "Customer was not found.");
        }

        customer.Activate();
        await customers.SaveAsync(customer, cancellationToken);
        return Result<Customer>.Success(customer);
    }

    public async Task<Result<Customer>> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<Customer>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Customer ID cannot be empty.");
        }

        var customer = await customers.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return Result<Customer>.Failure(
                ApplicationErrorCode.NotFound,
                "Customer was not found.");
        }

        customer.Deactivate();
        await customers.SaveAsync(customer, cancellationToken);
        return Result<Customer>.Success(customer);
    }
}
