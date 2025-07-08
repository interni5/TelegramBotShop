using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;


public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;


    public ApiService(HttpClient httpClient, IOptions<ApiSettings> apiSettings)
    {
        _httpClient = httpClient;
        _apiSettings = apiSettings.Value;


        _httpClient.BaseAddress = new Uri(_apiSettings.BaseUrl); // Устанавливаем базовый URL для HttpClient. Это позволяет указывать только относительные пути в методах GetAsync, PostAsync и т.д.

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiSettings.ApiKey);

    }

    public async Task<string> GetDataAsync()
    {
        var responce = await _httpClient.GetAsync("endpoint");
        responce.EnsureSuccessStatusCode();
        return await responce.Content.ReadAsStringAsync();
    }

    public async Task<decimal> GetCNYRateAscync()
    {
        var responce = await _httpClient.GetAsync("");
        responce.EnsureSuccessStatusCode();

        var json = await responce.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        if (document.RootElement.TryGetProperty("conversion_rates", out var rates))
        {
            if (rates.TryGetProperty("CNY", out var cnyRate))
            {
                return cnyRate.GetDecimal();
            }
        }

        throw new KeyNotFoundException("CNY курс не найден");

    }
}