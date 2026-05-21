namespace FitPanel.Services;

public class NutritionResult
{
    public string Name { get; set; } = string.Empty;
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double Carbs { get; set; }
    public double Fats { get; set; }
}

public interface INutritionApiService
{
    Task<NutritionResult?> GetNutritionAsync(string query);
}
