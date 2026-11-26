namespace ContosoInventory.Shared.DTOs;

/// <summary>
/// Supplies a positive quantity to add to a product's stock.
/// </summary>
public class RestockProductDto
{
    /// <summary>
    /// Gets or sets the quantity to add.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}
