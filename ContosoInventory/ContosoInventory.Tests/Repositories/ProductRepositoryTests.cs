using ContosoInventory.Server.Data;
using ContosoInventory.Server.Models;
using ContosoInventory.Server.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContosoInventory.Tests.Repositories;

/// <summary>
/// Tests product repository behavior against SQLite.
/// </summary>
public class ProductRepositoryTests
{
    /// <summary>
    /// Confirms concurrent restock updates preserve every quantity increment.
    /// </summary>
    [Fact]
    public async Task TryRestockAsync_ConcurrentCalls_AddsEveryQuantity()
    {
        await using var database = await CreateDatabaseAsync();

        const int requestCount = 12;
        var updates = Enumerable.Range(0, requestCount).Select(async _ =>
        {
            await using var context = new InventoryContext(database.Options);
            var repository = new ProductRepository(context, NullLogger<ProductRepository>.Instance);
            return await repository.TryRestockAsync(database.ProductId, 1, DateTime.UtcNow);
        });

        var results = await Task.WhenAll(updates);

        Assert.All(results, result => Assert.True(result));
        await using var verificationContext = new InventoryContext(database.Options);
        var product = await verificationContext.Products.SingleAsync(item => item.Id == database.ProductId);
        Assert.Equal(requestCount, product.StockQuantity);
    }

    /// <summary>
    /// Converts a duplicate case-insensitive SKU constraint into a business error.
    /// </summary>
    [Fact]
    public async Task AddAsync_DuplicateSkuIgnoringCase_ThrowsInvalidOperationException()
    {
        await using var database = await CreateDatabaseAsync();
        await using var context = new InventoryContext(database.Options);
        var repository = new ProductRepository(context, NullLogger<ProductRepository>.Instance);
        var duplicate = CreateProduct("sku-1");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.AddAsync(duplicate));

        Assert.Contains("sku-1", exception.Message.ToLowerInvariant());
    }

    /// <summary>
    /// Converts an update-time duplicate SKU constraint into a business error.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_DuplicateSkuOnUpdate_ThrowsInvalidOperationException()
    {
        await using var database = await CreateDatabaseAsync();
        await using var setupContext = new InventoryContext(database.Options);
        var categoryId = await setupContext.Categories
            .Select(category => category.Id)
            .SingleAsync();
        var secondProduct = CreateProduct("SKU-2");
        secondProduct.CategoryId = categoryId;
        setupContext.Products.Add(secondProduct);
        await setupContext.SaveChangesAsync();

        await using var updateContext = new InventoryContext(database.Options);
        var product = await updateContext.Products.SingleAsync(item => item.Id == secondProduct.Id);
        product.Sku = "sku-1";
        var repository = new ProductRepository(updateContext, NullLogger<ProductRepository>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.SaveChangesAsync());

        Assert.Contains("sku-1", exception.Message.ToLowerInvariant());
    }

    private static async Task<TestDatabase> CreateDatabaseAsync()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"products-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath};Default Timeout=30;Pooling=False";

        var options = new DbContextOptionsBuilder<InventoryContext>()
            .UseSqlite(connectionString)
            .Options;

        await using var context = new InventoryContext(options);
        await context.Database.EnsureCreatedAsync();

        var category = new Category
        {
            Name = "Computers",
            Description = "Computer equipment",
            DisplayOrder = 1,
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = CreateProduct("SKU-1");
        product.CategoryId = category.Id;
        context.Products.Add(product);
        await context.SaveChangesAsync();

        return new TestDatabase(databasePath, options, product.Id);
    }

    private static Product CreateProduct(string sku)
    {
        return new Product
        {
            Name = "Laptop",
            Sku = sku,
            Description = "Portable computer",
            Price = 100,
            StockQuantity = 0,
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow
        };
    }

    private sealed record TestDatabase(
        string DatabasePath,
        DbContextOptions<InventoryContext> Options,
        int ProductId) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            if (File.Exists(DatabasePath))
            {
                File.Delete(DatabasePath);
            }

            return ValueTask.CompletedTask;
        }
    }
}
