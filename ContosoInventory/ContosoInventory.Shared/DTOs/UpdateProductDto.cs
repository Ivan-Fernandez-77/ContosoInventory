namespace ContosoInventory.Shared.DTOs;

/// <summary>
/// Supplies the data used to update an existing product.
/// </summary>
public class UpdateProductDto
{
    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stock keeping unit.
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Sku { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product description.
    /// </summary>
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product price, which must be greater than zero.
    /// </summary>
    [Range(1E-28, double.MaxValue)]
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the stock quantity.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    /// <summary>
    /// Gets or sets the product category identifier.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}
