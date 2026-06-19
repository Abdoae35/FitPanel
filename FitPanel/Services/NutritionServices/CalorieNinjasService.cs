using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FitPanel.Services;

public class CalorieNinjasService : INutritionApiService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public CalorieNinjasService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _apiKey = configuration["CalorieNinjas:ApiKey"] ?? throw new InvalidOperationException("CalorieNinjas:ApiKey is not configured.");
    }

    public async Task<NutritionResult?> GetNutritionAsync(string query)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.calorieninjas.com/v1/nutrition?query={Uri.EscapeDataString(query)}");
        request.Headers.Add("X-Api-Key", _apiKey);

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var data = await response.Content.ReadFromJsonAsync<CalorieNinjasResponse>();
        if (data == null || !data.Items.Any())
            return null;

        // Sum up the items (e.g., "3 eggs and 100g chicken")
        return new NutritionResult
        {
            Name = query,
            Calories = data.Items.Sum(x => x.Calories),
            Protein = data.Items.Sum(x => x.Protein),
            Carbs = data.Items.Sum(x => x.Carbs),
            Fats = data.Items.Sum(x => x.Fats)
        };
    }

    private class CalorieNinjasResponse
    {
        [JsonPropertyName("items")]
        public List<CalorieNinjasItem> Items { get; set; } = new();
    }

    private class CalorieNinjasItem
    {
        [JsonPropertyName("calories")]
        public double Calories { get; set; }
        [JsonPropertyName("protein_g")]
        public double Protein { get; set; }
        [JsonPropertyName("carbohydrates_total_g")]
        public double Carbs { get; set; }
        [JsonPropertyName("fat_total_g")]
        public double Fats { get; set; }
    }
}
