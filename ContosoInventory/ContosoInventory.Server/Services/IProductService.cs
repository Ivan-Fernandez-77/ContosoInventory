using ContosoInventory.Shared.DTOs;

namespace ContosoInventory.Server.Services;

/// <summary>
/// Defines business operations for inventory products.
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Retrieves all products, optionally filtered by category.
    /// </summary>
    Task<List<ProductResponseDto>> GetAllProductsAsync(int? categoryId);

    /// <summary>
    /// Retrieves a product by its identifier.
    /// </summary>
    Task<ProductResponseDto?> GetProductByIdAsync(int id);

    /// <summary>
    /// Creates a product after validating its fields and category.
    /// </summary>
    Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto);

    /// <summary>
    /// Updates a product, or returns null if it does not exist.
    /// </summary>
    Task<ProductResponseDto?> UpdateProductAsync(int id, UpdateProductDto dto);

    /// <summary>
    /// Deletes a product, returning false if it does not exist.
    /// </summary>
    Task<bool> DeleteProductAsync(int id);

    /// <summary>
    /// Adds stock to a product, or returns null if it does not exist.
    /// </summary>
    Task<ProductResponseDto?> RestockProductAsync(int id, RestockProductDto dto);
}
