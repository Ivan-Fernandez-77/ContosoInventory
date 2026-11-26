using ContosoInventory.Server.Models;
using ContosoInventory.Server.Repositories;
using ContosoInventory.Server.Services;
using ContosoInventory.Shared.DTOs;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ContosoInventory.Tests.Services;

/// <summary>
/// Tests product service validation and repository interactions.
/// </summary>
public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly ProductService _productService;

    /// <summary>
    /// Creates a product service with a mocked repository.
    /// </summary>
    public ProductServiceTests()
    {
        _productService = new ProductService(
            _productRepository.Object,
            NullLogger<ProductService>.Instance);
    }

    /// <summary>
    /// Rejects a product with an empty description.
    /// </summary>
    [Fact]
    public async Task CreateProductAsync_EmptyDescription_ThrowsArgumentException()
    {
        var dto = CreateProduct(description: "");

        await Assert.ThrowsAsync<ArgumentException>(() => _productService.CreateProductAsync(dto));

        _productRepository.Verify(repository => repository.CategoryExistsAsync(It.IsAny<int>()), Times.Never);
        _productRepository.Verify(repository => repository.AddAsync(It.IsAny<Product>()), Times.Never);
    }

    /// <summary>
    /// Rejects a product with a zero price.
    /// </summary>
    [Fact]
    public async Task CreateProductAsync_ZeroPrice_ThrowsArgumentException()
    {
        var dto = CreateProduct(price: 0);

        await Assert.ThrowsAsync<ArgumentException>(() => _productService.CreateProductAsync(dto));

        _productRepository.Verify(repository => repository.CategoryExistsAsync(It.IsAny<int>()), Times.Never);
        _productRepository.Verify(repository => repository.AddAsync(It.IsAny<Product>()), Times.Never);
    }

    /// <summary>
    /// Rejects a SKU already assigned to another product.
    /// </summary>
    [Fact]
    public async Task CreateProductAsync_DuplicateSku_ThrowsInvalidOperationException()
    {
        _productRepository.Setup(repository => repository.CategoryExistsAsync(3)).ReturnsAsync(true);
        _productRepository.Setup(repository => repository.SkuExistsAsync("SKU-1", null)).ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _productService.CreateProductAsync(CreateProduct()));

        Assert.Contains("SKU-1", exception.Message);
        _productRepository.Verify(repository => repository.AddAsync(It.IsAny<Product>()), Times.Never);
    }

    /// <summary>
    /// Rejects a product assigned to a category that does not exist.
    /// </summary>
    [Fact]
    public async Task CreateProductAsync_CategoryDoesNotExist_ThrowsInvalidOperationException()
    {
        _productRepository.Setup(repository => repository.CategoryExistsAsync(3)).ReturnsAsync(false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _productService.CreateProductAsync(CreateProduct()));

        _productRepository.Verify(repository => repository.SkuExistsAsync(It.IsAny<string>(), null), Times.Never);
        _productRepository.Verify(repository => repository.AddAsync(It.IsAny<Product>()), Times.Never);
    }

    /// <summary>
    /// Passes an optional category filter to the repository.
    /// </summary>
    [Fact]
    public async Task GetAllProductsAsync_CategoryFilter_PassesCategoryIdToRepository()
    {
        _productRepository
            .Setup(repository => repository.GetAllAsync(3))
            .ReturnsAsync(new List<Product>
            {
                new()
                {
                    Id = 5,
                    Name = "Monitor",
                    Sku = "MON-5",
                    Description = "Display",
                    Price = 100,
                    StockQuantity = 4,
                    CategoryId = 3
                }
            });

        var products = await _productService.GetAllProductsAsync(3);

        Assert.Single(products);
        Assert.Equal(3, products[0].CategoryId);
        _productRepository.Verify(repository => repository.GetAllAsync(3), Times.Once);
    }

    /// <summary>
    /// Uses an atomic repository update when restocking a product.
    /// </summary>
    [Fact]
    public async Task RestockProductAsync_ProductExists_UsesAtomicRepositoryUpdate()
    {
        var updatedProduct = new Product
        {
            Id = 7,
            Name = "Laptop",
            Sku = "LAP-7",
            Description = "Portable computer",
            Price = 800,
            StockQuantity = 12,
            CategoryId = 3,
            LastUpdatedDate = DateTime.UtcNow
        };
        _productRepository
            .Setup(repository => repository.TryRestockAsync(7, 5, It.IsAny<DateTime>()))
            .ReturnsAsync(true);
        _productRepository.Setup(repository => repository.GetByIdAsync(7)).ReturnsAsync(updatedProduct);

        var response = await _productService.RestockProductAsync(7, new RestockProductDto { Quantity = 5 });

        Assert.NotNull(response);
        Assert.Equal(12, response.StockQuantity);
        _productRepository.Verify(
            repository => repository.TryRestockAsync(7, 5, It.IsAny<DateTime>()),
            Times.Once);
        _productRepository.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    /// <summary>
    /// Rejects a restock that would overflow the stock quantity.
    /// </summary>
    [Fact]
    public async Task RestockProductAsync_StockWouldOverflow_ThrowsArgumentException()
    {
        _productRepository
            .Setup(repository => repository.TryRestockAsync(7, 5, It.IsAny<DateTime>()))
            .ReturnsAsync(false);
        _productRepository
            .Setup(repository => repository.GetByIdAsync(7))
            .ReturnsAsync(new Product { Id = 7, StockQuantity = int.MaxValue });

        await Assert.ThrowsAsync<ArgumentException>(
            () => _productService.RestockProductAsync(7, new RestockProductDto { Quantity = 5 }));
    }

    private static CreateProductDto CreateProduct(
        string description = "Portable computer",
        decimal price = 100)
    {
        return new CreateProductDto
        {
            Name = "Laptop",
            Sku = "SKU-1",
            Description = description,
            Price = price,
            StockQuantity = 2,
            CategoryId = 3
        };
    }
}
