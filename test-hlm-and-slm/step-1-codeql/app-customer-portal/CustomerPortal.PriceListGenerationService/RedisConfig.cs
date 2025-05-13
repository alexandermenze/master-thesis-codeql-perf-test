using StackExchange.Redis;

namespace CustomerPortal.PriceListGenerationService;

public record RedisConfig(string TasksStreamName, string ConsumerGroupName);
