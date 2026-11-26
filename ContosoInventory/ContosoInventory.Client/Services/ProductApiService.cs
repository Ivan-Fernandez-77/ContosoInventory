using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ContosoInventory.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace ContosoInventory.Client.Services;

/// <summary>
/// Sends validated requests to the product API.
/// </summary>
public class ProductApiService : IProductApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductApiService> _logger;

    /// <summary>
    /// Initializes a product API client.
    /// </summary>
    public ProductApiService(HttpClient httpClient, ILogger<ProductApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ProductResponseDto>> GetAllProductsAsync(int? categoryId = null)
    {
        if (categoryId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(categoryId), "Category ID must be greater than zero.");
        }

        var path = categoryId.HasValue
            ? $"/api/products?categoryId={categoryId.Value}"
            : "/api/products";
        return await SendForResponseAsync<List<ProductResponseDto>>(
            () => _httpClient.GetAsync(path),
            "retrieving products")
            ?? throw new InvalidDataException("The product API returned an empty product list response.");
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto?> GetProductByIdAsync(int id)
    {
        ValidateId(id, nameof(id));
        return await SendForResponseAsync<ProductResponseDto>(
            () => _httpClient.GetAsync($"/api/products/{id}"),
            $"retrieving product {id}",
            returnNullOnNotFound: true);
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto?> CreateProductAsync(CreateProductDto dto)
    {
        ValidateDto(dto, nameof(dto));
        return await SendForResponseAsync<ProductResponseDto>(
            () => _httpClient.PostAsJsonAsync("/api/products", dto),
            "creating a product");
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto?> UpdateProductAsync(int id, UpdateProductDto dto)
    {
        ValidateId(id, nameof(id));
        ValidateDto(dto, nameof(dto));
        return await SendForResponseAsync<ProductResponseDto>(
            () => _httpClient.PutAsJsonAsync($"/api/products/{id}", dto),
            $"updating product {id}",
            returnNullOnNotFound: true);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteProductAsync(int id)
    {
        ValidateId(id, nameof(id));

        try
        {
            using var response = await _httpClient.DeleteAsync($"/api/products/{id}");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            await EnsureSuccessStatusCodeAsync(response);
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Product API request failed while deleting product {ProductId}.", id);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Product API request was canceled while deleting product {ProductId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto?> RestockProductAsync(int id, RestockProductDto dto)
    {
        ValidateId(id, nameof(id));
        ValidateDto(dto, nameof(dto));
        return await SendForResponseAsync<ProductResponseDto>(
            () => _httpClient.PostAsJsonAsync($"/api/products/{id}/restock", dto),
            $"restocking product {id}",
            returnNullOnNotFound: true);
    }

    private async Task<T?> SendForResponseAsync<T>(
        Func<Task<HttpResponseMessage>> sendRequest,
        string operation,
        bool returnNullOnNotFound = false)
        where T : class
    {
        try
        {
            using var response = await sendRequest();
            if (returnNullOnNotFound && response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            await EnsureSuccessStatusCodeAsync(response);
            return await response.Content.ReadFromJsonAsync<T>()
                ?? throw new InvalidDataException($"The product API returned an empty response while {operation}.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Product API request failed while {Operation}.", operation);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Product API returned invalid JSON while {Operation}.", operation);
            throw;
        }
        catch (InvalidDataException ex)
        {
            _logger.LogError(ex, "Product API returned an invalid response while {Operation}.", operation);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Product API request was canceled while {Operation}.", operation);
            throw;
        }
    }

    private static async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = await GetErrorMessageAsync(response);
        throw new HttpRequestException(message, null, response.StatusCode);
    }

    private static async Task<string> GetErrorMessageAsync(HttpResponseMessage response)
    {
        if ((int)response.StatusCode >= 500)
        {
            return "The product service encountered an error. Please try again later.";
        }

        var content = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                using var document = JsonDocument.Parse(content);
                if (document.RootElement.ValueKind == JsonValueKind.Object &&
                    document.RootElement.TryGetProperty("message", out var messageElement) &&
                    messageElement.ValueKind == JsonValueKind.String)
                {
                    var message = messageElement.GetString();
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        return message;
                    }
                }
            }
            catch (JsonException)
            {
                return $"Product API request failed with status code {(int)response.StatusCode}.";
            }
        }

        return $"Product API request failed with status code {(int)response.StatusCode}.";
    }

    private static void ValidateId(int id, string parameterName)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Product ID must be greater than zero.");
        }
    }

    private static void ValidateDto<T>(T dto, string parameterName)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(dto, parameterName);

        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, new ValidationContext(dto), validationResults, validateAllProperties: true))
        {
            var message = string.Join(" ", validationResults.Select(result => result.ErrorMessage));
            throw new ArgumentException(message, parameterName);
        }
    }
}
