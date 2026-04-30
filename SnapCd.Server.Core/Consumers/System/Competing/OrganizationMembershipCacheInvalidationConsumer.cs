using MassTransit;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Services.OrganizationContext;

namespace SnapCd.Server.Core.Consumers.System.Competing;

public class OrganizationMembershipCacheInvalidationConsumer :
    IConsumer<OrganizationUserUpdatedEvent>,
    IConsumer<OrganizationUserDeletedEvent>
{
    private readonly OrganizationMembershipCacheService _cacheService;
    private readonly ILogger<OrganizationMembershipCacheInvalidationConsumer> _logger;

    public OrganizationMembershipCacheInvalidationConsumer(
        OrganizationMembershipCacheService cacheService,
        ILogger<OrganizationMembershipCacheInvalidationConsumer> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrganizationUserUpdatedEvent> context)
    {
        var data = context.Message.Data;
        await _cacheService.InvalidateAsync(data.UserId, context.Message.OrganizationId);
        _logger.LogDebug("Invalidated membership cache for user {UserId} in org {OrganizationId} (updated)",
            data.UserId, context.Message.OrganizationId);
    }

    public async Task Consume(ConsumeContext<OrganizationUserDeletedEvent> context)
    {
        var data = context.Message.Data;
        await _cacheService.InvalidateAsync(data.UserId, context.Message.OrganizationId);
        _logger.LogDebug("Invalidated membership cache for user {UserId} in org {OrganizationId} (deleted)",
            data.UserId, context.Message.OrganizationId);
    }
}
