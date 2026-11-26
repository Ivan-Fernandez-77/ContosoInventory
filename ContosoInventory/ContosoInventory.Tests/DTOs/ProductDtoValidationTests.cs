using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ContosoInventory.Shared.DTOs;
using Xunit;

namespace ContosoInventory.Tests.DTOs;

/// <summary>
/// Tests product DTO validation across cultures.
/// </summary>
public class ProductDtoValidationTests
{
    /// <summary>
    /// Accepts a valid decimal price when the current culture uses a comma decimal separator.
    /// </summary>
    [Fact]
    public void CreateProductDto_NonInvariantCulture_ValidPriceIsAccepted()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-ES");
            var validationResults = new List<ValidationResult>();
            var dto = CreateProductDto(price: 12.34m);

            var isValid = Validator.TryValidateObject(
                dto,
                new ValidationContext(dto),
                validationResults,
                validateAllProperties: true);

            Assert.True(isValid, string.Join("; ", validationResults.Select(result => result.ErrorMessage)));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    /// <summary>
    /// Rejects a zero price.
    /// </summary>
    [Fact]
    public void CreateProductDto_ZeroPrice_IsRejected()
    {
        var dto = CreateProductDto(price: 0);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            dto,
            new ValidationContext(dto),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(dto.Price)));
    }

    private static CreateProductDto CreateProductDto(decimal price)
    {
        return new CreateProductDto
        {
            Name = "Laptop",
            Sku = "LAP-1",
            Description = "Portable computer",
            Price = price,
            StockQuantity = 4,
            CategoryId = 1
        };
    }
}
