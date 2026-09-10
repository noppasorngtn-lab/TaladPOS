namespace TaladPOS.Api.IntegrationTests.Helpers;

/// <summary>Builds the multipart/form-data body POST/PUT /products expects (contracts/products.md).</summary>
public static class MultipartFormBuilder
{
    public static MultipartFormDataContent ProductForm(
        string name, decimal price, int quantityOnHand, string? barcode = null, int? lowStockThreshold = null)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(name), "Name" },
            { new StringContent(price.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Price" },
            { new StringContent(quantityOnHand.ToString(System.Globalization.CultureInfo.InvariantCulture)), "QuantityOnHand" },
        };

        if (barcode is not null)
        {
            form.Add(new StringContent(barcode), "Barcode");
        }

        if (lowStockThreshold is not null)
        {
            form.Add(new StringContent(lowStockThreshold.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)), "LowStockThreshold");
        }

        return form;
    }
}
