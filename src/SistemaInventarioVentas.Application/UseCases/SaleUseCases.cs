using System.Text.Json;
using SistemaInventarioVentas.Application.Common;
using SistemaInventarioVentas.Application.Exceptions;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Application.UseCases;

public sealed class SaleUseCases(
    ISaleRepository sales,
    IProductRepository? products = null,
    IInventoryRepository? inventories = null,
    ICustomerRepository? customers = null,
    ICurrentUser? currentUser = null)
{
    public Task<Result<Sale>> CreateDraftAsync(
        string customerReference,
        Guid userId,
        DateTimeOffset? createdAtUtc = null,
        CancellationToken cancellationToken = default) =>
        CreateDraftAsync(customerReference, userId, customerId: null, createdAtUtc, cancellationToken);

    public async Task<Result<Sale>> CreateDraftAsync(
        string? customerReference,
        Guid userId,
        Guid? customerId,
        DateTimeOffset? createdAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.InvalidInput,
                "User ID cannot be empty.");
        }

        string resolvedCustomerReference;

        if (customerId.HasValue)
        {
            if (customerId.Value == Guid.Empty)
            {
                return Result<Sale>.Failure(
                    ApplicationErrorCode.InvalidInput,
                    "Customer ID cannot be empty.");
            }

            if (customers is null)
            {
                throw new InvalidOperationException("Customer repository is not configured.");
            }

            var customer = await customers.GetByIdAsync(customerId.Value, cancellationToken);
            if (customer is null)
            {
                return Result<Sale>.Failure(
                    ApplicationErrorCode.NotFound,
                    "Customer was not found.");
            }

            if (!customer.IsActive)
            {
                return Result<Sale>.Failure(
                    ApplicationErrorCode.InvalidInput,
                    "Cannot create a sale for an inactive customer.");
            }

            resolvedCustomerReference = customer.Name;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(customerReference))
            {
                return Result<Sale>.Failure(
                    ApplicationErrorCode.InvalidInput,
                    "Customer reference is required.");
            }

            resolvedCustomerReference = customerReference.Trim();
        }

        Sale sale;
        try
        {
            sale = new Sale(resolvedCustomerReference, userId, createdAtUtc, customerId);
        }
        catch (ArgumentException exception)
        {
            return Result<Sale>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }

        var auditEvent = CreateAuditEvent(
            AuditEvent.EventTypes.SaleCreated,
            AuditEvent.EntityNames.Sale,
            sale.Id.ToString(),
            sale.CreatedAtUtc,
            SerializeSaleCreatedMetadata(sale));

        await sales.AddAsync(sale, auditEvent, cancellationToken);
        return Result<Sale>.Success(sale);
    }

    public async Task<Result<Sale>> UpdateCustomerAsync(
        Guid saleId,
        Guid? customerId,
        string? customerReference = null,
        CancellationToken cancellationToken = default)
    {
        if (saleId == Guid.Empty)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Sale ID cannot be empty.");
        }

        var sale = await sales.GetByIdAsync(saleId, cancellationToken);
        if (sale is null)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.NotFound,
                "Sale was not found.");
        }

        if (sale.Status != SaleStatus.Draft)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.InvalidInput,
                $"Cannot modify customer of a sale in '{sale.Status}' status.");
        }

        if (customerId.HasValue)
        {
            if (customerId.Value == Guid.Empty)
            {
                return Result<Sale>.Failure(
                    ApplicationErrorCode.InvalidInput,
                    "Customer ID cannot be empty.");
            }

            if (customers is null)
            {
                throw new InvalidOperationException("Customer repository is not configured.");
            }

            var customer = await customers.GetByIdAsync(customerId.Value, cancellationToken);
            if (customer is null)
            {
                return Result<Sale>.Failure(
                    ApplicationErrorCode.NotFound,
                    "Customer was not found.");
            }

            if (!customer.IsActive)
            {
                return Result<Sale>.Failure(
                    ApplicationErrorCode.InvalidInput,
                    "Cannot assign an inactive customer to a sale.");
            }

            sale.AssignCustomer(customer.Id, customer.Name);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(customerReference))
            {
                return Result<Sale>.Failure(
                    ApplicationErrorCode.InvalidInput,
                    "Customer reference is required when removing customer association.");
            }

            sale.ClearCustomer(customerReference);
        }

        await sales.SaveAsync(sale, cancellationToken);
        return Result<Sale>.Success(sale);
    }

    public async Task<Result<Sale>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Sale ID cannot be empty.");
        }

        var sale = await sales.GetByIdAsync(id, cancellationToken);
        if (sale is null)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.NotFound,
                "Sale was not found.");
        }

        return Result<Sale>.Success(sale);
    }

    public Task<Result<Sale>> AddDetailAsync(
        Guid saleId,
        string productIdentifier,
        int quantity,
        decimal unitPrice,
        decimal taxRate,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeIdentifier(productIdentifier, out var parsedIdentifier, out var parseError))
        {
            return Task.FromResult(Result<Sale>.Failure(ApplicationErrorCode.InvalidInput, parseError));
        }

        return AddDetailCoreAsync(saleId, parsedIdentifier, quantity, unitPrice, taxRate, cancellationToken);
    }

    public Task<Result<Sale>> AddDetailAsync(
        Guid saleId,
        ProductIdentifier productIdentifier,
        int quantity,
        decimal unitPrice,
        decimal taxRate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(productIdentifier);
        return AddDetailCoreAsync(saleId, productIdentifier, quantity, unitPrice, taxRate, cancellationToken);
    }

    public Task<Result<Sale>> UpdateDetailAsync(
        Guid saleId,
        string productIdentifier,
        int newQuantity,
        decimal? newUnitPrice = null,
        decimal? newTaxRate = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeIdentifier(productIdentifier, out var parsedIdentifier, out var parseError))
        {
            return Task.FromResult(Result<Sale>.Failure(ApplicationErrorCode.InvalidInput, parseError));
        }

        return UpdateDetailCoreAsync(saleId, parsedIdentifier, newQuantity, newUnitPrice, newTaxRate, cancellationToken);
    }

    public Task<Result<Sale>> UpdateDetailAsync(
        Guid saleId,
        ProductIdentifier productIdentifier,
        int newQuantity,
        decimal? newUnitPrice = null,
        decimal? newTaxRate = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(productIdentifier);
        return UpdateDetailCoreAsync(saleId, productIdentifier, newQuantity, newUnitPrice, newTaxRate, cancellationToken);
    }

    public async Task<Result<Sale>> UpdateDetailAsync(
        Guid saleId,
        Guid detailId,
        int newQuantity,
        decimal? newUnitPrice = null,
        decimal? newTaxRate = null,
        CancellationToken cancellationToken = default)
    {
        if (saleId == Guid.Empty)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Sale ID cannot be empty.");
        }

        if (detailId == Guid.Empty)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Sale detail ID cannot be empty.");
        }

        var sale = await sales.GetByIdAsync(saleId, cancellationToken);
        if (sale is null)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.NotFound,
                "Sale was not found.");
        }

        try
        {
            sale.UpdateDetail(detailId, newQuantity, newUnitPrice, newTaxRate);
        }
        catch (ArgumentException exception)
        {
            return Result<Sale>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            var code = exception.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? ApplicationErrorCode.NotFound
                : ApplicationErrorCode.InvalidInput;

            return Result<Sale>.Failure(code, exception.Message);
        }

        await sales.SaveAsync(sale, cancellationToken);
        return Result<Sale>.Success(sale);
    }

    public Task<Result<Sale>> RemoveDetailAsync(
        Guid saleId,
        string productIdentifier,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeIdentifier(productIdentifier, out var parsedIdentifier, out var parseError))
        {
            return Task.FromResult(Result<Sale>.Failure(ApplicationErrorCode.InvalidInput, parseError));
        }

        return RemoveDetailCoreAsync(saleId, parsedIdentifier, cancellationToken);
    }

    public Task<Result<Sale>> RemoveDetailAsync(
        Guid saleId,
        ProductIdentifier productIdentifier,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(productIdentifier);
        return RemoveDetailCoreAsync(saleId, productIdentifier, cancellationToken);
    }

    public async Task<Result<Sale>> RemoveDetailAsync(
        Guid saleId,
        Guid detailId,
        CancellationToken cancellationToken = default)
    {
        if (saleId == Guid.Empty)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Sale ID cannot be empty.");
        }

        if (detailId == Guid.Empty)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Sale detail ID cannot be empty.");
        }

        var sale = await sales.GetByIdAsync(saleId, cancellationToken);
        if (sale is null)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.NotFound,
                "Sale was not found.");
        }

        try
        {
            var removed = sale.RemoveDetail(detailId);
            if (!removed)
            {
                return Result<Sale>.Failure(
                    ApplicationErrorCode.NotFound,
                    "Sale detail was not found.");
            }
        }
        catch (InvalidOperationException exception)
        {
            return Result<Sale>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }

        await sales.SaveAsync(sale, cancellationToken);
        return Result<Sale>.Success(sale);
    }

    public async Task<Result<Sale>> UpdateCustomerReferenceAsync(
        Guid saleId,
        string customerReference,
        CancellationToken cancellationToken = default)
    {
        if (saleId == Guid.Empty)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Sale ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(customerReference))
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Customer reference is required.");
        }

        var sale = await sales.GetByIdAsync(saleId, cancellationToken);
        if (sale is null)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.NotFound,
                "Sale was not found.");
        }

        try
        {
            sale.UpdateCustomerReference(customerReference);
        }
        catch (ArgumentException exception)
        {
            return Result<Sale>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Result<Sale>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }

        await sales.SaveAsync(sale, cancellationToken);
        return Result<Sale>.Success(sale);
    }

    public async Task<Result<Sale>> ConfirmSaleAsync(
        Guid saleId,
        DateTimeOffset? confirmedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (saleId == Guid.Empty)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Sale ID cannot be empty.");
        }

        if (inventories is null)
        {
            throw new InvalidOperationException("Inventory repository is required to confirm a sale.");
        }

        var sale = await sales.GetByIdAsync(saleId, cancellationToken);
        if (sale is null)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.NotFound,
                "Sale was not found.");
        }

        if (sale.Status != SaleStatus.Draft)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.InvalidInput,
                $"A sale in '{sale.Status}' status cannot be confirmed.");
        }

        if (sale.Details.Count == 0)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.InvalidInput,
                "A sale must contain at least one detail line before it can be confirmed.");
        }

        var requiredQuantities = sale.Details
            .GroupBy(detail => detail.ProductIdentifier)
            .ToDictionary(group => group.Key, group => group.Sum(detail => detail.Quantity));

        var affectedInventories = new List<Inventory>(requiredQuantities.Count);

        foreach (var (productIdentifier, requiredQuantity) in requiredQuantities)
        {
            var inventory = await inventories.GetByProductIdentifierAsync(productIdentifier, cancellationToken);
            if (inventory is null || inventory.Quantity < requiredQuantity)
            {
                return Result<Sale>.Failure(
                    ApplicationErrorCode.InsufficientStock,
                    $"Insufficient stock for product '{productIdentifier.Value}'. Required: {requiredQuantity}, available: {inventory?.Quantity ?? 0}.");
            }

            affectedInventories.Add(inventory);
        }

        var effectiveConfirmedAtUtc = confirmedAtUtc ?? DateTimeOffset.UtcNow;

        try
        {
            sale.Confirm(effectiveConfirmedAtUtc);

            foreach (var detail in sale.Details)
            {
                var inventory = affectedInventories.First(i => i.ProductIdentifier == detail.ProductIdentifier);
                inventory.RegisterExit(detail.Quantity, $"Sale {sale.Id}", effectiveConfirmedAtUtc);
            }
        }
        catch (InvalidOperationException exception)
        {
            return Result<Sale>.Failure(ApplicationErrorCode.InsufficientStock, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return Result<Sale>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }

        var auditEvent = CreateAuditEvent(
            AuditEvent.EventTypes.SaleConfirmed,
            AuditEvent.EntityNames.Sale,
            sale.Id.ToString(),
            effectiveConfirmedAtUtc,
            SerializeSaleConfirmedMetadata(sale));

        try
        {
            await sales.ConfirmSaleAsync(sale, affectedInventories, auditEvent, cancellationToken);
        }
        catch (ConcurrencyException exception)
        {
            return Result<Sale>.Failure(ApplicationErrorCode.ConcurrencyConflict, exception.Message);
        }

        return Result<Sale>.Success(sale);
    }

    public Task<Result<Sale>> CancelSaleAsync(
        Guid saleId,
        CancellationToken cancellationToken = default) =>
        CancelSaleAsync(saleId, reason: null, cancellationToken);

    public async Task<Result<Sale>> CancelSaleAsync(
        Guid saleId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        if (saleId == Guid.Empty)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Sale ID cannot be empty.");
        }

        var sale = await sales.GetByIdAsync(saleId, cancellationToken);
        if (sale is null)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.NotFound,
                "Sale was not found.");
        }

        if (sale.Status == SaleStatus.Cancelled)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Sale is already cancelled.");
        }

        if (sale.Status == SaleStatus.Confirmed)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Cannot cancel a confirmed sale without inventory reversal.");
        }

        try
        {
            sale.Cancel();
        }
        catch (InvalidOperationException exception)
        {
            return Result<Sale>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }

        var auditEvent = CreateAuditEvent(
            AuditEvent.EventTypes.SaleCancelled,
            AuditEvent.EntityNames.Sale,
            sale.Id.ToString(),
            DateTimeOffset.UtcNow,
            SerializeSaleCancelledMetadata(sale, reason));

        try
        {
            await sales.SaveAsync(sale, auditEvent, cancellationToken);
        }
        catch (ConcurrencyException exception)
        {
            return Result<Sale>.Failure(ApplicationErrorCode.ConcurrencyConflict, exception.Message);
        }

        return Result<Sale>.Success(sale);
    }

    private async Task<Result<Sale>> AddDetailCoreAsync(
        Guid saleId,
        ProductIdentifier productIdentifier,
        int quantity,
        decimal unitPrice,
        decimal taxRate,
        CancellationToken cancellationToken)
    {
        if (saleId == Guid.Empty)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Sale ID cannot be empty.");
        }

        if (products is not null && !await products.IdentifierExistsAsync(productIdentifier, cancellationToken))
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.NotFound,
                "Product was not found.");
        }

        var sale = await sales.GetByIdAsync(saleId, cancellationToken);
        if (sale is null)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.NotFound,
                "Sale was not found.");
        }

        try
        {
            sale.AddDetail(productIdentifier, quantity, unitPrice, taxRate);
        }
        catch (ArgumentException exception)
        {
            return Result<Sale>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Result<Sale>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }

        await sales.SaveAsync(sale, cancellationToken);
        return Result<Sale>.Success(sale);
    }

    private async Task<Result<Sale>> UpdateDetailCoreAsync(
        Guid saleId,
        ProductIdentifier productIdentifier,
        int newQuantity,
        decimal? newUnitPrice,
        decimal? newTaxRate,
        CancellationToken cancellationToken)
    {
        if (saleId == Guid.Empty)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Sale ID cannot be empty.");
        }

        var sale = await sales.GetByIdAsync(saleId, cancellationToken);
        if (sale is null)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.NotFound,
                "Sale was not found.");
        }

        try
        {
            sale.UpdateDetail(productIdentifier, newQuantity, newUnitPrice, newTaxRate);
        }
        catch (ArgumentException exception)
        {
            return Result<Sale>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            var code = exception.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? ApplicationErrorCode.NotFound
                : ApplicationErrorCode.InvalidInput;

            return Result<Sale>.Failure(code, exception.Message);
        }

        await sales.SaveAsync(sale, cancellationToken);
        return Result<Sale>.Success(sale);
    }

    private async Task<Result<Sale>> RemoveDetailCoreAsync(
        Guid saleId,
        ProductIdentifier productIdentifier,
        CancellationToken cancellationToken)
    {
        if (saleId == Guid.Empty)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.InvalidInput,
                "Sale ID cannot be empty.");
        }

        var sale = await sales.GetByIdAsync(saleId, cancellationToken);
        if (sale is null)
        {
            return Result<Sale>.Failure(
                ApplicationErrorCode.NotFound,
                "Sale was not found.");
        }

        try
        {
            var removed = sale.RemoveDetail(productIdentifier);
            if (!removed)
            {
                return Result<Sale>.Failure(
                    ApplicationErrorCode.NotFound,
                    "Sale detail was not found.");
            }
        }
        catch (InvalidOperationException exception)
        {
            return Result<Sale>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }

        await sales.SaveAsync(sale, cancellationToken);
        return Result<Sale>.Success(sale);
    }

    private static bool TryNormalizeIdentifier(
        string identifier,
        out ProductIdentifier parsedIdentifier,
        out string error)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            parsedIdentifier = null!;
            error = "Product identifier is required.";
            return false;
        }

        try
        {
            parsedIdentifier = new ProductIdentifier(identifier);
            error = string.Empty;
            return true;
        }
        catch (ArgumentException exception)
        {
            parsedIdentifier = null!;
            error = exception.Message;
            return false;
        }
    }

    private AuditEvent CreateAuditEvent(
        string eventType,
        string entityName,
        string? entityId,
        DateTimeOffset occurredAtUtc,
        string? metadataJson)
    {
        var isAuthenticated = currentUser?.IsAuthenticated ?? false;
        var userId = currentUser?.UserId;

        return isAuthenticated && userId.HasValue && userId.Value != Guid.Empty
            ? AuditEvent.CreateForUser(
                userId.Value,
                eventType,
                entityName,
                entityId,
                occurredAtUtc,
                metadataJson)
            : AuditEvent.CreateForSystem(
                eventType,
                entityName,
                entityId,
                occurredAtUtc,
                metadataJson);
    }

    private static string SerializeSaleCreatedMetadata(Sale sale)
    {
        var payload = new Dictionary<string, object?>
        {
            ["customerReference"] = sale.CustomerReference,
            ["status"] = sale.Status.ToString()
        };

        if (sale.CustomerId.HasValue)
        {
            payload["customerId"] = sale.CustomerId.Value.ToString();
        }

        return JsonSerializer.Serialize(payload);
    }

    private static string SerializeSaleConfirmedMetadata(Sale sale)
    {
        var payload = new Dictionary<string, object?>
        {
            ["customerReference"] = sale.CustomerReference,
            ["subtotal"] = sale.Subtotal,
            ["taxAmount"] = sale.TaxAmount,
            ["total"] = sale.Total,
            ["itemCount"] = sale.Details.Count,
            ["status"] = sale.Status.ToString()
        };

        if (sale.CustomerId.HasValue)
        {
            payload["customerId"] = sale.CustomerId.Value.ToString();
        }

        return JsonSerializer.Serialize(payload);
    }

    private static string SerializeSaleCancelledMetadata(Sale sale, string? reason)
    {
        var payload = new Dictionary<string, object?>
        {
            ["customerReference"] = sale.CustomerReference,
            ["status"] = sale.Status.ToString()
        };

        if (!string.IsNullOrWhiteSpace(reason))
        {
            payload["reason"] = reason.Trim();
        }

        if (sale.CustomerId.HasValue)
        {
            payload["customerId"] = sale.CustomerId.Value.ToString();
        }

        return JsonSerializer.Serialize(payload);
    }
}
