namespace CustomerPortal.Messages.Events;

public record CustomerPriceListGeneratedEvent(
    Guid Id,
    Guid CommandId,
    string SalesOrg,
    DateOnly PriceDate,
    string StorageFilePath
);
