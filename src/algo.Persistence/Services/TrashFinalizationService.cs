using algo.Application.Common.Trash;
using algo.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace algo.Persistence.Services;

internal sealed class TrashFinalizationService(
    IServiceScopeFactory scopeFactory,
    ILogger<TrashFinalizationService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FinalizeExpiredTrashAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to finalize expired trash items.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task FinalizeExpiredTrashAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var utcNow = DateTimeOffset.UtcNow;
        var utcNowDateTime = utcNow.UtcDateTime;

        var users = (await db.Users
            .IgnoreQueryFilters()
            .Where(user =>
                user.TrashedAt != null &&
                user.TrashExpiresAt != null &&
                user.DeletedAt == null)
            .ToListAsync(cancellationToken))
            .Where(user => user.TrashExpiresAt <= utcNow)
            .ToList();

        var roles = (await db.Roles
            .IgnoreQueryFilters()
            .Where(role =>
                role.TrashedAt != null &&
                role.TrashExpiresAt != null &&
                role.DeletedAt == null)
            .ToListAsync(cancellationToken))
            .Where(role => role.TrashExpiresAt <= utcNow)
            .ToList();

        var policies = (await db.AccessPolicies
            .IgnoreQueryFilters()
            .Where(policy =>
                policy.TrashedAt != null &&
                policy.TrashExpiresAt != null &&
                policy.DeletedAt == null)
            .ToListAsync(cancellationToken))
            .Where(policy => policy.TrashExpiresAt <= utcNowDateTime)
            .ToList();

        var products = (await db.Products
            .IgnoreQueryFilters()
            .Where(product =>
                product.TrashedAt != null &&
                product.TrashExpiresAt != null &&
                product.DeletedAt == null)
            .ToListAsync(cancellationToken))
            .Where(product => product.TrashExpiresAt <= utcNow)
            .ToList();

        var categories = (await db.Categories
            .IgnoreQueryFilters()
            .Where(category =>
                category.TrashedAt != null &&
                category.TrashExpiresAt != null &&
                category.DeletedAt == null)
            .ToListAsync(cancellationToken))
            .Where(category => category.TrashExpiresAt <= utcNow)
            .ToList();

        if (users.Count == 0 &&
            roles.Count == 0 &&
            policies.Count == 0 &&
            products.Count == 0 &&
            categories.Count == 0)
        {
            return;
        }

        foreach (var user in users)
        {
            user.DeletedAt = utcNow;
        }

        foreach (var role in roles)
        {
            role.DeletedAt = utcNow;
        }

        foreach (var policy in policies)
        {
            policy.DeletedAt = utcNowDateTime;
        }

        foreach (var product in products)
        {
            product.DeletedAt = utcNow;
            product.UpdatedAt = utcNow;
        }

        foreach (var category in categories)
        {
            category.DeletedAt = utcNow;
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Finalized trashed records after {RetentionDays} days. Users={UserCount}, Roles={RoleCount}, Policies={PolicyCount}, Products={ProductCount}, Categories={CategoryCount}",
            TrashRetention.Duration.TotalDays,
            users.Count,
            roles.Count,
            policies.Count,
            products.Count,
            categories.Count);
    }
}
