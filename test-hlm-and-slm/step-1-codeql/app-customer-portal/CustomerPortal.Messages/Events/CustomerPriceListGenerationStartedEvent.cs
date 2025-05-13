namespace CustomerPortal.Messages.Events;

public record CustomerPriceListGenerationStartedEvent(
    Guid Id,
    Guid CommandId,
    string SalesOrg,
    DateOnly PriceDate
);
