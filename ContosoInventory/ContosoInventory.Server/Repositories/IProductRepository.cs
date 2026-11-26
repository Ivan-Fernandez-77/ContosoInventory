using ContosoInventory.Server.Models;

namespace ContosoInventory.Server.Repositories;

/// <summary>
/// Defines persistence operations for products.
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Retrieves products, optionally filtered by category.
    /// </summary>
    Task<List<Product>> GetAllAsync(int? categoryId);

    /// <summary>
    /// Retrieves a product without tracking it for updates.
    /// </summary>
    Task<Product?> GetByIdAsync(int id);

    /// <summary>
    /// Retrieves a product tracked for modification.
    /// </summary>
    Task<Product?> GetByIdForUpdateAsync(int id);

    /// <summary>
    /// Determines whether the category exists.
    /// </summary>
    Task<bool> CategoryExistsAsync(int categoryId);

    /// <summary>
    /// Determines whether any products reference the category.
    /// </summary>
    Task<bool> HasProductsInCategoryAsync(int categoryId);

    /// <summary>
    /// Determines whether the SKU is already assigned to another product.
    /// </summary>
    Task<bool> SkuExistsAsync(string sku, int? exceptProductId = null);

    /// <summary>
    /// Adds and persists a product.
    /// </summary>
    Task AddAsync(Product product);

    /// <summary>
    /// Persists tracked changes.
    /// </summary>
    Task SaveChangesAsync();

    /// <summary>
    /// Atomically increases a product's stock unless the result would overflow.
    /// </summary>
    Task<bool> TryRestockAsync(int id, int quantity, DateTime lastUpdatedDate);

    /// <summary>
    /// Removes and persists deletion of a product.
    /// </summary>
    Task DeleteAsync(Product product);
}
