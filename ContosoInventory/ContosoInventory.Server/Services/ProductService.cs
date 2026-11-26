using ContosoInventory.Server.Models;
using ContosoInventory.Server.Repositories;
using ContosoInventory.Shared.DTOs;

namespace ContosoInventory.Server.Services;

/// <summary>
/// Implements product validation, business rules, and DTO mapping.
/// </summary>
public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductService> _logger;

    /// <summary>
    /// Initializes the product service.
    /// </summary>
    public ProductService(IProductRepository productRepository, ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ProductResponseDto>> GetAllProductsAsync(int? categoryId)
    {
        if (categoryId is <= 0)
        {
            throw new ArgumentException("Category ID must be greater than zero.", nameof(categoryId));
        }

        var products = await _productRepository.GetAllAsync(categoryId);
        return products.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto?> GetProductByIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Product ID must be greater than zero.", nameof(id));
        }

        var product = await _productRepository.GetByIdAsync(id);
        return product == null ? null : MapToDto(product);
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateProductFields(dto.Name, dto.Sku, dto.Description, dto.Price, dto.StockQuantity, dto.CategoryId);

        if (!await _productRepository.CategoryExistsAsync(dto.CategoryId))
        {
            throw new InvalidOperationException($"Category with ID {dto.CategoryId} does not exist.");
        }

        if (await _productRepository.SkuExistsAsync(dto.Sku))
        {
            throw new InvalidOperationException($"A product with SKU '{dto.Sku}' already exists.");
        }

        var now = DateTime.UtcNow;
        var product = new Product
        {
            Name = dto.Name,
            Sku = dto.Sku,
            Description = dto.Description,
            Price = dto.Price,
            StockQuantity = dto.StockQuantity,
            CategoryId = dto.CategoryId,
            CreatedDate = now,
            LastUpdatedDate = now
        };

        await _productRepository.AddAsync(product);
        _logger.LogInformation("Product created with ID {ProductId} and SKU {ProductSku}.", product.Id, product.Sku);
        return MapToDto(product);
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto?> UpdateProductAsync(int id, UpdateProductDto dto)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Product ID must be greater than zero.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(dto);
        ValidateProductFields(dto.Name, dto.Sku, dto.Description, dto.Price, dto.StockQuantity, dto.CategoryId);

        var product = await _productRepository.GetByIdForUpdateAsync(id);
        if (product == null)
        {
            return null;
        }

        if (!await _productRepository.CategoryExistsAsync(dto.CategoryId))
        {
            throw new InvalidOperationException($"Category with ID {dto.CategoryId} does not exist.");
        }

        if (await _productRepository.SkuExistsAsync(dto.Sku, id))
        {
            throw new InvalidOperationException($"A product with SKU '{dto.Sku}' already exists.");
        }

        product.Name = dto.Name;
        product.Sku = dto.Sku;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.StockQuantity = dto.StockQuantity;
        product.CategoryId = dto.CategoryId;
        product.LastUpdatedDate = DateTime.UtcNow;

        await _productRepository.SaveChangesAsync();
        _logger.LogInformation("Product updated with ID {ProductId} and SKU {ProductSku}.", product.Id, product.Sku);
        return MapToDto(product);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteProductAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Product ID must be greater than zero.", nameof(id));
        }

        var product = await _productRepository.GetByIdForUpdateAsync(id);
        if (product == null)
        {
            return false;
        }

        await _productRepository.DeleteAsync(product);
        _logger.LogInformation("Product deleted with ID {ProductId}.", id);
        return true;
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto?> RestockProductAsync(int id, RestockProductDto dto)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Product ID must be greater than zero.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Quantity <= 0)
        {
            throw new ArgumentException("Restock quantity must be greater than zero.", nameof(dto));
        }

        var updated = await _productRepository.TryRestockAsync(id, dto.Quantity, DateTime.UtcNow);
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
        {
            return null;
        }

        if (!updated)
        {
            throw new ArgumentException("Restock quantity exceeds the maximum supported stock level.", nameof(dto));
        }

        _logger.LogInformation("Product {ProductId} restocked by {Quantity}.", id, dto.Quantity);
        return MapToDto(product);
    }

    private static void ValidateProductFields(
        string? name,
        string? sku,
        string? description,
        decimal price,
        int stockQuantity,
        int categoryId)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length is < 2 or > 200)
        {
            throw new ArgumentException("Product name is required and must be between 2 and 200 characters.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(sku) || sku.Length > 50)
        {
            throw new ArgumentException("SKU is required and cannot exceed 50 characters.", nameof(sku));
        }

        if (string.IsNullOrWhiteSpace(description) || description.Length > 1000)
        {
            throw new ArgumentException("Description is required and cannot exceed 1000 characters.", nameof(description));
        }

        if (price <= 0)
        {
            throw new ArgumentException("Price must be greater than zero.", nameof(price));
        }

        if (stockQuantity < 0)
        {
            throw new ArgumentException("Stock quantity cannot be negative.", nameof(stockQuantity));
        }

        if (categoryId <= 0)
        {
            throw new ArgumentException("Category ID must be greater than zero.", nameof(categoryId));
        }
    }

    private static ProductResponseDto MapToDto(Product product)
    {
        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Sku = product.Sku,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            CreatedDate = product.CreatedDate,
            LastUpdatedDate = product.LastUpdatedDate
        };
    }
}
