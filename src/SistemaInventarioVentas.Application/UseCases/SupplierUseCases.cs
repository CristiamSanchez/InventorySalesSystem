using SistemaInventarioVentas.Application.Common;
using SistemaInventarioVentas.Application.Exceptions;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Application.UseCases;

public sealed class SupplierUseCases(ISupplierRepository suppliers)
{
    public async Task<Result<Supplier>> CreateAsync(
        string name,
        string? contactName = null,
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
                return Result<Supplier>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
            }
        }

        string? normalizedIdentification = null;
        if (!string.IsNullOrWhiteSpace(identificationNumber))
        {
            var trimmed = identificationNumber.Trim().ToUpperInvariant();
            if (trimmed.Length > 50)
            {
                return Result<Supplier>.Failure(
                    ApplicationErrorCode.InvalidInput,
                    "Identification number cannot exceed 50 characters.");
            }

            if (await suppliers.ExistsByIdentificationNumberAsync(trimmed, cancellationToken: cancellationToken))
            {
                return Result<Supplier>.Failure(
                    ApplicationErrorCode.SupplierIdentificationConflict,
                    "A supplier with this identification number already exists.");
            }

            normalizedIdentification = trimmed;
        }

        Supplier supplier;
        try
        {
            supplier = new Supplier(
                name: name,
                contactName: contactName,
                identificationNumber: normalizedIdentification,
                email: emailAddress,
                phone: phone,
                address: address,
                createdAtUtc: createdAtUtc);
        }
        catch (ArgumentException exception)
        {
            return Result<Supplier>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }

        try
        {
            await suppliers.AddAsync(supplier, cancellationToken);
        }
        catch (DuplicateSupplierIdentificationException exception)
        {
            return Result<Supplier>.Failure(
                ApplicationErrorCode.SupplierIdentificationConflict,
                exception.Message);
        }

        return Result<Supplier>.Success(supplier);
    }

    public async Task<Result<Supplier>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<Supplier>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Supplier ID cannot be empty.");
        }

        var supplier = await suppliers.GetByIdAsync(id, cancellationToken);
        return supplier is null
            ? Result<Supplier>.Failure(ApplicationErrorCode.NotFound, "Supplier was not found.")
            : Result<Supplier>.Success(supplier);
    }

    public async Task<Result<IReadOnlyList<Supplier>>> ListAsync(
        SupplierQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveQuery = query ?? new SupplierQuery();
        var normalizedQuery = effectiveQuery with { SearchTerm = effectiveQuery.SearchTerm?.Trim() };
        var result = await suppliers.ListAsync(normalizedQuery, cancellationToken);
        return Result<IReadOnlyList<Supplier>>.Success(result);
    }

    public async Task<Result<Supplier>> UpdateDetailsAsync(
        Guid id,
        string name,
        string? contactName = null,
        string? identificationNumber = null,
        string? email = null,
        string? phone = null,
        string? address = null,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<Supplier>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Supplier ID cannot be empty.");
        }

        var supplier = await suppliers.GetByIdAsync(id, cancellationToken);
        if (supplier is null)
        {
            return Result<Supplier>.Failure(
                ApplicationErrorCode.NotFound,
                "Supplier was not found.");
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
                return Result<Supplier>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
            }
        }

        string? normalizedIdentification = null;
        if (!string.IsNullOrWhiteSpace(identificationNumber))
        {
            var trimmed = identificationNumber.Trim().ToUpperInvariant();
            if (trimmed.Length > 50)
            {
                return Result<Supplier>.Failure(
                    ApplicationErrorCode.InvalidInput,
                    "Identification number cannot exceed 50 characters.");
            }

            if (await suppliers.ExistsByIdentificationNumberAsync(trimmed, excludingId: id, cancellationToken: cancellationToken))
            {
                return Result<Supplier>.Failure(
                    ApplicationErrorCode.SupplierIdentificationConflict,
                    "A supplier with this identification number already exists.");
            }

            normalizedIdentification = trimmed;
        }

        try
        {
            supplier.UpdateDetails(
                name: name,
                contactName: contactName,
                identificationNumber: normalizedIdentification,
                email: emailAddress,
                phone: phone,
                address: address);
        }
        catch (ArgumentException exception)
        {
            return Result<Supplier>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }

        try
        {
            await suppliers.SaveAsync(supplier, cancellationToken);
        }
        catch (DuplicateSupplierIdentificationException exception)
        {
            return Result<Supplier>.Failure(
                ApplicationErrorCode.SupplierIdentificationConflict,
                exception.Message);
        }

        return Result<Supplier>.Success(supplier);
    }

    public async Task<Result<Supplier>> ActivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<Supplier>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Supplier ID cannot be empty.");
        }

        var supplier = await suppliers.GetByIdAsync(id, cancellationToken);
        if (supplier is null)
        {
            return Result<Supplier>.Failure(
                ApplicationErrorCode.NotFound,
                "Supplier was not found.");
        }

        supplier.Activate();
        await suppliers.SaveAsync(supplier, cancellationToken);
        return Result<Supplier>.Success(supplier);
    }

    public async Task<Result<Supplier>> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<Supplier>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Supplier ID cannot be empty.");
        }

        var supplier = await suppliers.GetByIdAsync(id, cancellationToken);
        if (supplier is null)
        {
            return Result<Supplier>.Failure(
                ApplicationErrorCode.NotFound,
                "Supplier was not found.");
        }

        supplier.Deactivate();
        await suppliers.SaveAsync(supplier, cancellationToken);
        return Result<Supplier>.Success(supplier);
    }
}
