using System.Reflection;
using ContosoInventory.Server.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace ContosoInventory.Tests.Controllers;

/// <summary>
/// Tests the authorization attributes on product API endpoints.
/// </summary>
public class ProductsControllerAuthorizationTests
{
    /// <summary>
    /// Verifies authentication is required by the product controller.
    /// </summary>
    [Fact]
    public void Controller_RequiresAuthenticationForAllEndpoints()
    {
        var authorizeAttribute = typeof(ProductsController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorizeAttribute);
    }

    /// <summary>
    /// Verifies product mutation endpoints require the Admin role.
    /// </summary>
    /// <param name="actionName">The controller action name.</param>
    [Theory]
    [InlineData(nameof(ProductsController.CreateProduct))]
    [InlineData(nameof(ProductsController.UpdateProduct))]
    [InlineData(nameof(ProductsController.DeleteProduct))]
    [InlineData(nameof(ProductsController.RestockProduct))]
    public void MutationEndpoints_RequireAdminRole(string actionName)
    {
        var method = typeof(ProductsController).GetMethod(actionName);

        Assert.NotNull(method);
        var authorizeAttribute = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttribute);
        Assert.Equal("Admin", authorizeAttribute!.Roles);
    }
}
