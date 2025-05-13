namespace CustomerPortal.Messages.Commands;

public record GenerateCustomerPriceListCommand(
    Guid Id,
    Guid UserId,
    int CustomerNo,
    string SalesOrg,
    DateOnly PriceDate
);
