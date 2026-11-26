using ContosoInventory.Shared.DTOs;

namespace ContosoInventory.Client.Services;

/// <summary>
/// Defines client operations for the product API.
/// </summary>
public interface IProductApiService
{
    /// <summary>
    /// Retrieves products, optionally filtered by category.
    /// </summary>
    Task<List<ProductResponseDto>> GetAllProductsAsync(int? categoryId = null);

    /// <summary>
    /// Retrieves a product by its identifier.
    /// </summary>
    Task<ProductResponseDto?> GetProductByIdAsync(int id);

    /// <summary>
    /// Creates a product.
    /// </summary>
    Task<ProductResponseDto?> CreateProductAsync(CreateProductDto dto);

    /// <summary>
    /// Updates a product.
    /// </summary>
    Task<ProductResponseDto?> UpdateProductAsync(int id, UpdateProductDto dto);

    /// <summary>
    /// Deletes a product.
    /// </summary>
    Task<bool> DeleteProductAsync(int id);

    /// <summary>
    /// Adds the requested quantity to a product's stock.
    /// </summary>
    Task<ProductResponseDto?> RestockProductAsync(int id, RestockProductDto dto);
}
