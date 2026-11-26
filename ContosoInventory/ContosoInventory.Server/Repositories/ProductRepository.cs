using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using ContosoInventory.Server.Data;
using ContosoInventory.Server.Models;

namespace ContosoInventory.Server.Repositories;

/// <summary>
/// Implements product persistence using Entity Framework Core.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly InventoryContext _context;
    private readonly ILogger<ProductRepository> _logger;

    /// <summary>
    /// Initializes a repository for product persistence.
    /// </summary>
    public ProductRepository(InventoryContext context, ILogger<ProductRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<Product>> GetAllAsync(int? categoryId)
    {
        try
        {
            var query = _context.Products.AsNoTracking();
            if (categoryId.HasValue)
            {
                query = query.Where(product => product.CategoryId == categoryId.Value);
            }

            return await query
                .OrderBy(product => product.Name)
                .ThenBy(product => product.Id)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products for category {CategoryId}.", categoryId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Product?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(product => product.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product with ID {ProductId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Product?> GetByIdForUpdateAsync(int id)
    {
        try
        {
            return await _context.Products.FirstOrDefaultAsync(product => product.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product with ID {ProductId} for update.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> CategoryExistsAsync(int categoryId)
    {
        try
        {
            return await _context.Categories.AnyAsync(category => category.Id == categoryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking category with ID {CategoryId}.", categoryId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasProductsInCategoryAsync(int categoryId)
    {
        try
        {
            return await _context.Products.AnyAsync(product => product.CategoryId == categoryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking products for category {CategoryId}.", categoryId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SkuExistsAsync(string sku, int? exceptProductId = null)
    {
        try
        {
            return await _context.Products.AnyAsync(product =>
                product.Sku == sku &&
                (!exceptProductId.HasValue || product.Id != exceptProductId.Value));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking SKU {ProductSku}.", sku);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task AddAsync(Product product)
    {
        try
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsSkuUniqueViolation(ex))
        {
            _logger.LogWarning(ex, "A product with SKU {ProductSku} already exists.", product.Sku);
            throw new InvalidOperationException($"A product with SKU '{product.Sku}' already exists.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding product with SKU {ProductSku}.", product.Sku);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsSkuUniqueViolation(ex))
        {
            var sku = _context.ChangeTracker.Entries<Product>()
                .FirstOrDefault(entry => entry.State is EntityState.Added or EntityState.Modified)
                ?.Entity.Sku;
            _logger.LogWarning(ex, "A product with SKU {ProductSku} already exists.", sku);
            throw new InvalidOperationException(
                sku == null
                    ? "A product with the specified SKU already exists."
                    : $"A product with SKU '{sku}' already exists.",
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving product changes.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryRestockAsync(int id, int quantity, DateTime lastUpdatedDate)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Restock quantity must be greater than zero.");
        }

        try
        {
            var affectedRows = await _context.Products
                .Where(product =>
                    product.Id == id &&
                    product.StockQuantity <= int.MaxValue - quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(product => product.StockQuantity, product => product.StockQuantity + quantity)
                    .SetProperty(product => product.LastUpdatedDate, lastUpdatedDate));

            return affectedRows == 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restocking product {ProductId} by {Quantity}.", id, quantity);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Product product)
    {
        try
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product with ID {ProductId}.", product.Id);
            throw;
        }
    }

    private static bool IsSkuUniqueViolation(DbUpdateException exception)
    {
        return exception.GetBaseException() is SqliteException sqliteException &&
            sqliteException.SqliteExtendedErrorCode == 2067 &&
            sqliteException.Message.Contains("Products.Sku", StringComparison.OrdinalIgnoreCase);
    }
}
