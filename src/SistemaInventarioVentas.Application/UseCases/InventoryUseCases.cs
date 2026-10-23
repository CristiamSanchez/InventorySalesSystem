using System.Text.Json;
using SistemaInventarioVentas.Application.Common;
using SistemaInventarioVentas.Application.Exceptions;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Application.UseCases;

public sealed class InventoryUseCases(
    IInventoryRepository inventories,
    IProductRepository? products = null,
    ISupplierRepository? suppliers = null,
    ICurrentUser? currentUser = null)
{
    public Task<Result<Inventory>> RecordEntryAsync(
        string productIdentifier,
        int quantity,
        string reference,
        DateTimeOffset? occurredAtUtc = null,
        CancellationToken cancellationToken = default) =>
        RecordEntryAsync(productIdentifier, quantity, reference, occurredAtUtc, supplierId: null, cancellationToken);

    public Task<Result<Inventory>> RecordEntryAsync(
        string productIdentifier,
        int quantity,
        string reference,
        DateTimeOffset? occurredAtUtc,
        Guid? supplierId,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeIdentifier(productIdentifier, out var parsedIdentifier, out var parseError))
        {
            return Task.FromResult(Result<Inventory>.Failure(ApplicationErrorCode.InvalidInput, parseError));
        }

        return RecordEntryCoreAsync(parsedIdentifier, quantity, reference, occurredAtUtc, supplierId, cancellationToken);
    }

    public Task<Result<Inventory>> RecordEntryAsync(
        ProductIdentifier productIdentifier,
        int quantity,
        string reference,
        DateTimeOffset? occurredAtUtc = null,
        CancellationToken cancellationToken = default) =>
        RecordEntryAsync(productIdentifier, quantity, reference, occurredAtUtc, supplierId: null, cancellationToken);

    public Task<Result<Inventory>> RecordEntryAsync(
        ProductIdentifier productIdentifier,
        int quantity,
        string reference,
        DateTimeOffset? occurredAtUtc,
        Guid? supplierId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(productIdentifier);
        return RecordEntryCoreAsync(productIdentifier, quantity, reference, occurredAtUtc, supplierId, cancellationToken);
    }

    public Task<Result<Inventory>> RecordExitAsync(
        string productIdentifier,
        int quantity,
        string reference,
        DateTimeOffset? occurredAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeIdentifier(productIdentifier, out var parsedIdentifier, out var parseError))
        {
            return Task.FromResult(Result<Inventory>.Failure(ApplicationErrorCode.InvalidInput, parseError));
        }

        return RecordExitCoreAsync(parsedIdentifier, quantity, reference, occurredAtUtc, cancellationToken);
    }

    public Task<Result<Inventory>> RecordExitAsync(
        ProductIdentifier productIdentifier,
        int quantity,
        string reference,
        DateTimeOffset? occurredAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(productIdentifier);
        return RecordExitCoreAsync(productIdentifier, quantity, reference, occurredAtUtc, cancellationToken);
    }

    public Task<Result<Inventory>> AdjustAsync(
        string productIdentifier,
        int quantityDelta,
        string reason,
        DateTimeOffset? occurredAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeIdentifier(productIdentifier, out var parsedIdentifier, out var parseError))
        {
            return Task.FromResult(Result<Inventory>.Failure(ApplicationErrorCode.InvalidInput, parseError));
        }

        return AdjustCoreAsync(parsedIdentifier, quantityDelta, reason, occurredAtUtc, cancellationToken);
    }

    public Task<Result<Inventory>> AdjustAsync(
        ProductIdentifier productIdentifier,
        int quantityDelta,
        string reason,
        DateTimeOffset? occurredAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(productIdentifier);
        return AdjustCoreAsync(productIdentifier, quantityDelta, reason, occurredAtUtc, cancellationToken);
    }

    public Task<Result<Inventory>> GetByProductIdentifierAsync(
        string productIdentifier,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeIdentifier(productIdentifier, out var parsedIdentifier, out var parseError))
        {
            return Task.FromResult(Result<Inventory>.Failure(ApplicationErrorCode.InvalidInput, parseError));
        }

        return GetByProductIdentifierCoreAsync(parsedIdentifier, cancellationToken);
    }

    public Task<Result<Inventory>> GetByProductIdentifierAsync(
        ProductIdentifier productIdentifier,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(productIdentifier);
        return GetByProductIdentifierCoreAsync(productIdentifier, cancellationToken);
    }

    public Task<Result<Inventory>> GetAsync(
        string productIdentifier,
        CancellationToken cancellationToken = default) =>
        GetByProductIdentifierAsync(productIdentifier, cancellationToken);

    public Task<Result<Inventory>> GetAsync(
        ProductIdentifier productIdentifier,
        CancellationToken cancellationToken = default) =>
        GetByProductIdentifierAsync(productIdentifier, cancellationToken);

    public Task<Result<IReadOnlyList<InventoryMovement>>> GetMovementsAsync(
        string productIdentifier,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeIdentifier(productIdentifier, out var parsedIdentifier, out var parseError))
        {
            return Task.FromResult(Result<IReadOnlyList<InventoryMovement>>.Failure(ApplicationErrorCode.InvalidInput, parseError));
        }

        return GetMovementsCoreAsync(parsedIdentifier, cancellationToken);
    }

    public Task<Result<IReadOnlyList<InventoryMovement>>> GetMovementsAsync(
        ProductIdentifier productIdentifier,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(productIdentifier);
        return GetMovementsCoreAsync(productIdentifier, cancellationToken);
    }

    private async Task<Result<Inventory>> RecordEntryCoreAsync(
        ProductIdentifier productIdentifier,
        int quantity,
        string reference,
        DateTimeOffset? occurredAtUtc,
        Guid? supplierId,
        CancellationToken cancellationToken)
    {
        if (products is not null && !await products.IdentifierExistsAsync(productIdentifier, cancellationToken))
        {
            return Result<Inventory>.Failure(ApplicationErrorCode.NotFound, "Product was not found.");
        }

        string? supplierName = null;
        if (supplierId.HasValue)
        {
            if (supplierId.Value == Guid.Empty)
            {
                return Result<Inventory>.Failure(ApplicationErrorCode.InvalidInput, "Supplier ID cannot be empty.");
            }

            if (suppliers is null)
            {
                throw new InvalidOperationException("Supplier repository is required when recording an entry with a supplier.");
            }

            var supplier = await suppliers.GetByIdAsync(supplierId.Value, cancellationToken);
            if (supplier is null)
            {
                return Result<Inventory>.Failure(ApplicationErrorCode.NotFound, "Supplier was not found.");
            }

            if (!supplier.IsActive)
            {
                return Result<Inventory>.Failure(
                    ApplicationErrorCode.InvalidInput,
                    "Inactive suppliers cannot be used for recording new inventory entries.");
            }

            supplierName = supplier.Name;
        }

        var inventory = await inventories.GetByProductIdentifierAsync(productIdentifier, cancellationToken);
        var isNew = inventory is null;
        inventory ??= new Inventory(productIdentifier);

        try
        {
            inventory.RegisterEntry(quantity, reference, occurredAtUtc, supplierId, supplierName);
        }
        catch (ArgumentException exception)
        {
            return Result<Inventory>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Result<Inventory>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }

        var entryMovement = inventory.Movements.Last();
        var entryAuditEvent = CreateAuditEvent(
            AuditEvent.EventTypes.InventoryMovementRecorded,
            AuditEvent.EntityNames.InventoryMovement,
            entryMovement.Id.ToString(),
            entryMovement.OccurredAtUtc,
            SerializeEntryMetadata(productIdentifier.Value, quantity, reference, supplierId, supplierName));

        try
        {
            if (isNew)
            {
                await inventories.AddAsync(inventory, entryAuditEvent, cancellationToken);
            }
            else
            {
                await inventories.SaveAsync(inventory, entryAuditEvent, cancellationToken);
            }
        }
        catch (ConcurrencyException exception)
        {
            return Result<Inventory>.Failure(ApplicationErrorCode.ConcurrencyConflict, exception.Message);
        }

        return Result<Inventory>.Success(inventory);
    }

    private async Task<Result<Inventory>> RecordExitCoreAsync(
        ProductIdentifier productIdentifier,
        int quantity,
        string reference,
        DateTimeOffset? occurredAtUtc,
        CancellationToken cancellationToken)
    {
        if (products is not null && !await products.IdentifierExistsAsync(productIdentifier, cancellationToken))
        {
            return Result<Inventory>.Failure(ApplicationErrorCode.NotFound, "Product was not found.");
        }

        var inventory = await inventories.GetByProductIdentifierAsync(productIdentifier, cancellationToken);
        if (inventory is null)
        {
            return Result<Inventory>.Failure(ApplicationErrorCode.NotFound, "Inventory was not found.");
        }

        try
        {
            inventory.RegisterExit(quantity, reference, occurredAtUtc);
        }
        catch (ArgumentException exception)
        {
            return Result<Inventory>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Result<Inventory>.Failure(ApplicationErrorCode.InsufficientStock, exception.Message);
        }

        var exitMovement = inventory.Movements.Last();
        var exitAuditEvent = CreateAuditEvent(
            AuditEvent.EventTypes.InventoryMovementRecorded,
            AuditEvent.EntityNames.InventoryMovement,
            exitMovement.Id.ToString(),
            exitMovement.OccurredAtUtc,
            SerializeExitMetadata(productIdentifier.Value, quantity, reference));

        try
        {
            await inventories.SaveAsync(inventory, exitAuditEvent, cancellationToken);
        }
        catch (ConcurrencyException exception)
        {
            return Result<Inventory>.Failure(ApplicationErrorCode.ConcurrencyConflict, exception.Message);
        }

        return Result<Inventory>.Success(inventory);
    }

    private async Task<Result<Inventory>> AdjustCoreAsync(
        ProductIdentifier productIdentifier,
        int quantityDelta,
        string reason,
        DateTimeOffset? occurredAtUtc,
        CancellationToken cancellationToken)
    {
        if (products is not null && !await products.IdentifierExistsAsync(productIdentifier, cancellationToken))
        {
            return Result<Inventory>.Failure(ApplicationErrorCode.NotFound, "Product was not found.");
        }

        var inventory = await inventories.GetByProductIdentifierAsync(productIdentifier, cancellationToken);
        if (inventory is null)
        {
            return Result<Inventory>.Failure(ApplicationErrorCode.NotFound, "Inventory was not found.");
        }

        try
        {
            inventory.Adjust(quantityDelta, reason, occurredAtUtc);
        }
        catch (ArgumentException exception)
        {
            return Result<Inventory>.Failure(ApplicationErrorCode.InvalidInput, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Result<Inventory>.Failure(ApplicationErrorCode.InsufficientStock, exception.Message);
        }

        var adjustMovement = inventory.Movements.Last();
        var adjustAuditEvent = CreateAuditEvent(
            AuditEvent.EventTypes.InventoryMovementRecorded,
            AuditEvent.EntityNames.InventoryMovement,
            adjustMovement.Id.ToString(),
            adjustMovement.OccurredAtUtc,
            SerializeAdjustMetadata(productIdentifier.Value, quantityDelta, reason));

        try
        {
            await inventories.SaveAsync(inventory, adjustAuditEvent, cancellationToken);
        }
        catch (ConcurrencyException exception)
        {
            return Result<Inventory>.Failure(ApplicationErrorCode.ConcurrencyConflict, exception.Message);
        }

        return Result<Inventory>.Success(inventory);
    }

    private async Task<Result<Inventory>> GetByProductIdentifierCoreAsync(
        ProductIdentifier productIdentifier,
        CancellationToken cancellationToken)
    {
        var inventory = await inventories.GetByProductIdentifierAsync(productIdentifier, cancellationToken);
        return inventory is null
            ? Result<Inventory>.Failure(ApplicationErrorCode.NotFound, "Inventory was not found.")
            : Result<Inventory>.Success(inventory);
    }

    private async Task<Result<IReadOnlyList<InventoryMovement>>> GetMovementsCoreAsync(
        ProductIdentifier productIdentifier,
        CancellationToken cancellationToken)
    {
        var inventory = await inventories.GetByProductIdentifierAsync(productIdentifier, cancellationToken);
        if (inventory is null)
        {
            return Result<IReadOnlyList<InventoryMovement>>.Failure(ApplicationErrorCode.NotFound, "Inventory was not found.");
        }

        return Result<IReadOnlyList<InventoryMovement>>.Success(inventory.Movements.ToArray());
    }

    private static bool TryNormalizeIdentifier(
        string? identifier,
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

    private static string SerializeEntryMetadata(
        string productIdentifier,
        int quantity,
        string reference,
        Guid? supplierId,
        string? supplierName)
    {
        var payload = new Dictionary<string, object?>
        {
            ["productIdentifier"] = productIdentifier,
            ["movementType"] = "Entry",
            ["quantity"] = quantity,
            ["reference"] = reference
        };

        if (supplierId.HasValue)
        {
            payload["supplierId"] = supplierId.Value.ToString();
            payload["supplierName"] = supplierName;
        }

        return JsonSerializer.Serialize(payload);
    }

    private static string SerializeExitMetadata(
        string productIdentifier,
        int quantity,
        string reference)
    {
        var payload = new Dictionary<string, object?>
        {
            ["productIdentifier"] = productIdentifier,
            ["movementType"] = "Exit",
            ["quantity"] = quantity,
            ["reference"] = reference
        };

        return JsonSerializer.Serialize(payload);
    }

    private static string SerializeAdjustMetadata(
        string productIdentifier,
        int quantityDelta,
        string reason)
    {
        var payload = new Dictionary<string, object?>
        {
            ["productIdentifier"] = productIdentifier,
            ["movementType"] = "Adjustment",
            ["quantityDelta"] = quantityDelta,
            ["reason"] = reason
        };

        return JsonSerializer.Serialize(payload);
    }
}
