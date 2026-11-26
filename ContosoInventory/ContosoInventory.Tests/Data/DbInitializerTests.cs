using ContosoInventory.Server.Data;
using ContosoInventory.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ContosoInventory.Tests.Data;

/// <summary>
/// Tests startup database seeding.
/// </summary>
public class DbInitializerTests
{
    /// <summary>
    /// Seeds products linked to categories once for an empty database.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_EmptyDatabase_SeedsProductsIdempotently()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"initializer-{Guid.NewGuid():N}.db");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<InventoryContext>(options =>
            options.UseSqlite($"Data Source={databasePath};Pooling=False"));
        services.AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<InventoryContext>();

        using var serviceProvider = services.BuildServiceProvider();
        try
        {
            await InitializeAsync(serviceProvider);

            var seededProducts = await GetProductsAsync(serviceProvider);
            Assert.Equal(3, seededProducts.Count);
            Assert.All(seededProducts, product => Assert.True(product.Price > 0));

            var categoryIds = await GetCategoryIdsAsync(serviceProvider);
            Assert.All(seededProducts, product => Assert.Contains(product.CategoryId, categoryIds));

            await InitializeAsync(serviceProvider);

            var productsAfterSecondInitialization = await GetProductsAsync(serviceProvider);
            Assert.Equal(3, productsAfterSecondInitialization.Count);
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    private static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await DbInitializer.InitializeAsync(scope.ServiceProvider);
    }

    private static async Task<List<Product>> GetProductsAsync(IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryContext>();
        return await context.Products.AsNoTracking().ToListAsync();
    }

    private static async Task<List<int>> GetCategoryIdsAsync(IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryContext>();
        return await context.Categories.Select(category => category.Id).ToListAsync();
    }
}
