using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;
using FitPanel.Data.Models;
using FitPanel.Data.Enums;
using FitPanel.DTOs.Client;
using FitPanel.DTOs.Diet;
using FitPanel.DTOs.Workout;
using FitPanel.Services;

namespace FitPanel.Components.Pages.Dashboard;

public partial class ClientDetail
{
    [Parameter] public int ClientId { get; set; }
    private ClientResponseDto? client;
    private List<DietResponseDto> diets = new();
    private List<WorkoutResponseDto> workouts = new();
    private string coachId = string.Empty;
    private string activeTab = "diet";
    private bool isLoading = true;

    // ── Modals ────────────────────────────────────────────────────
    private bool showEditClientModal = false;
    private bool showDietModal = false;
    private bool showEditDietModal = false;
    private bool showMealModal = false;
    private bool showAlternativeModal = false;
    private bool showWorkoutModal = false;
    private bool showEditWorkoutModal = false;
    private bool showDayModal = false;
    private bool showExerciseModal = false;
    private bool showCardioModal = false;
    private bool showEditCardioModal = false;
    private bool showRenewModal = false;
    private bool showDeleteConfirm = false;

    // ── Edit Workout Day Notes ────────────────────────────────────
    private bool showEditWorkoutDayNotesModal = false;
    private int editWorkoutDayNotesWorkoutId = 0;
    private int editWorkoutDayNotesDayId = 0;
    private string? editWorkoutDayNotesText = "";

    // ── Edit Workout Day Warm-Up ──────────────────────────────────
    private bool showEditWarmUpModal = false;
    private int editWarmUpWorkoutId = 0;
    private int editWarmUpDayId = 0;
    private string? editWarmUpName = "";
    private string? editWarmUpLink = "";
    private List<CoachWarmUpDictionary> warmUpDict = new();


    // ── Templates ─────────────────────────────────────────────────
    private List<DietTemplate> dietTemplates = new();
    private List<WorkoutTemplate> workoutTemplates = new();
    private bool showSaveDietTemplateModal = false;
    private bool showSaveWorkoutTemplateModal = false;
    private bool showApplyDietTemplateModal = false;
    private bool showApplyWorkoutTemplateModal = false;
    private string dietTemplateName = "";
    private string workoutTemplateName = "";
    private int selectedDietTemplateId = 0;
    private int selectedWorkoutTemplateId = 0;
    private int savingTemplateDietId = 0;
    private int savingTemplateWorkoutId = 0;

    // ── Month accordion state ─────────────────────────────────────
    private HashSet<int> expandedDietMonths = new() { 1 };
    private HashSet<int> expandedWorkoutMonths = new() { 1 };

    // ── Edit client ───────────────────────────────────────────────
    private UpdateClientDto editClient = new();
    private bool isSavingClient = false;
    private string editClientMessage = string.Empty;
    private bool editClientSuccess = false;

    // ── Edit context ──────────────────────────────────────────────
    private int editMealId = 0;
    private int editExerciseId = 0;
    private int editAlternativeId = 0;
    private int activeDietId = 0;
    private int activeMealId = 0;
    private int activeWorkoutId = 0;
    private int activeDayId = 0;

    // ── Edit diet/workout count ───────────────────────────────────
    private int editingDietId = 0;
    private int editDietMealCount = 1;
    private int editDietCurrentCount = 0;
    private int editingWorkoutId = 0;
    private int editWorkoutDayCount = 1;
    private int editWorkoutCurrentCount = 0;

    // ── Edit Diet Notes ───────────────────────────────────────────
    private bool showEditDietNotesModal = false;
    private int editDietNotesId = 0;
    private string? editDietInstructions = null;

    // ── Delete confirm ────────────────────────────────────────────
    private string deleteConfirmType = string.Empty;
    private string deleteConfirmName = string.Empty;
    private string deleteConfirmMessage = string.Empty;
    private Func<Task>? deleteAction;

    // ── Renew ─────────────────────────────────────────────────────
    private int renewMonths = 1;
    private bool isRenewing = false;
    private string renewMessage = string.Empty;
    private bool renewSuccess = false;

    // ── PDF state ─────────────────────────────────────────────────
    private int generatingPdfDietId = 0;
    private int generatingPdfWorkoutId = 0;
    private string? pdfErrorMessage = null;

    // ── Form models ───────────────────────────────────────────────
    private CreateDietDto newDiet = new();
    private CreateMealItemDto newMeal = new();
    private CreateAlternativeDto newAlternative = new();
    private CreateWorkoutDto newWorkout = new();
    private CreateWorkoutDayDto newDay = new();
    private CreateExerciseDto newExercise = new();
    private CreateCardioDto newCardio = new();
    private CreateCardioDto editCardio = new();
    private int newDayEnum = 0;
    private string? dayValidationError = null;
    private int editingDayId = 0;
    private string editDayName = "";
    private int editDayEnum = 0;
    private string? editDayValidationError = null;
    private string? exerciseValidationError = null;
    private bool showEditDayModal = false;

    private int newCardioIntensity = 1;
    private int editCardioIntensity = 1;

    // ── Computed ──────────────────────────────────────────────────
    private static DateTime Today => DateTime.UtcNow.Date;
    private bool IsExpired => client != null && client.EndDate.ToUniversalTime().Date < Today;
    private bool IsExpiringSoon => client != null && !IsExpired && client.EndDate.ToUniversalTime().Date <= Today.AddDays(7);
    private int DaysLeft => client != null
        ? Math.Max(0, (client.EndDate.ToUniversalTime().Date - Today).Days)
        : 0;

    private HashSet<Days> UsedDaysForActiveWorkout =>
        workouts.FirstOrDefault(w => w.Id == activeWorkoutId)
                ?.WorkoutDays.Select(d => d.Day).ToHashSet()
        ?? new HashSet<Days>();

    private List<CoachMealDictionary> mealDict = new();
    private List<CoachExerciseDictionary> exerciseDict = new();
    private bool isSavingMeal = false;

    // ── Lifecycle ─────────────────────────────────────────────────

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var auth = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = await UserManager.GetUserAsync(auth.User);
            if (user == null) return;
            coachId = user.Id;
            client = await ClientService.GetClientByIdAsync(ClientId, coachId);
            if (client != null)
            {
                var dietsTask = DietService.GetDietsAsync(ClientId, coachId);
                var workoutsTask = WorkoutService.GetWorkoutsAsync(ClientId, coachId);
                var mealDictTask = DictionaryService.GetMealsAsync(coachId);
                var exerciseDictTask = DictionaryService.GetExercisesAsync(coachId);
                var warmUpDictTask = DictionaryService.GetWarmUpsAsync(coachId);
                var dietTemplatesTask = TemplateService.GetDietTemplatesAsync(coachId);
                var workoutTemplatesTask = TemplateService.GetWorkoutTemplatesAsync(coachId);

                await Task.WhenAll(dietsTask, workoutsTask, mealDictTask, exerciseDictTask, warmUpDictTask, dietTemplatesTask, workoutTemplatesTask);

                diets = await dietsTask;
                workouts = await workoutsTask;
                mealDict = await mealDictTask;
                exerciseDict = await exerciseDictTask;
                warmUpDict = await warmUpDictTask;
                dietTemplates = await dietTemplatesTask;
                workoutTemplates = await workoutTemplatesTask;
            }
        }
        finally
        {
            isLoading = false;
        }
    }

    // ── Month Accordion ───────────────────────────────────────────

    private void ToggleDietMonth(int month)
    {
        if (!expandedDietMonths.Add(month))
            expandedDietMonths.Remove(month);
    }

    private void ToggleWorkoutMonth(int month)
    {
        if (!expandedWorkoutMonths.Add(month))
            expandedWorkoutMonths.Remove(month);
    }

    // ── PDF ───────────────────────────────────────────────────────

    private async Task DownloadDietPdf(int dietId)
    {
        pdfErrorMessage = null;
        generatingPdfDietId = dietId;
        StateHasChanged();
        try
        {
            var bytes = await PdfService.GenerateDietPdfAsync(ClientId, dietId, coachId);
            if (bytes is null) { pdfErrorMessage = "Diet plan was not found in the database."; return; }
            var diet = diets.FirstOrDefault(d => d.Id == dietId);
            var base64 = Convert.ToBase64String(bytes);
            await JS.InvokeVoidAsync("fitpanel.downloadPdf",
                base64, $"{client!.Name}_diet_{diet!.CreatedAt:yyyy-MM-dd}.pdf");
        }
        catch (Exception ex)
        {
            pdfErrorMessage = $"Failed to generate PDF: {ex.Message}";
        }
        finally { generatingPdfDietId = 0; }
    }

    private async Task DownloadWorkoutPdf(int workoutId)
    {
        pdfErrorMessage = null;
        generatingPdfWorkoutId = workoutId;
        StateHasChanged();
        try
        {
            var bytes = await PdfService.GenerateWorkoutPdfAsync(ClientId, workoutId, coachId);
            if (bytes is null) { pdfErrorMessage = "Workout plan was not found in the database."; return; }
            var workout = workouts.FirstOrDefault(w => w.Id == workoutId);
            var base64 = Convert.ToBase64String(bytes);
            await JS.InvokeVoidAsync("fitpanel.downloadPdf",
                base64, $"{client!.Name}_workout_{workout!.CreatedAt:yyyy-MM-dd}.pdf");
        }
        catch (Exception ex)
        {
            pdfErrorMessage = $"Failed to generate PDF: {ex.Message}";
        }
        finally { generatingPdfWorkoutId = 0; }
    }

    // ── Edit Client ───────────────────────────────────────────────

    private void OpenEditClientModal()
    {
        editClient = new UpdateClientDto
        {
            Name = client!.Name,
            Weight = client.Weight,
            Bmr = client.Bmr,
            InbodyLink = client.InbodyLink,
            FromPicLink = client.FromPicLink,
            ToPicLink = client.ToPicLink,
            Goal = client.Goal,
            PhoneNumber = client.PhoneNumber
        };
        editClientMessage = string.Empty;
        showEditClientModal = true;
    }

    private async Task SaveClient()
    {
        if (client == null) return;
        isSavingClient = true;
        editClientMessage = string.Empty;
        var (success, message) = await ClientService.UpdateClientAsync(ClientId, editClient, coachId);
        isSavingClient = false;
        editClientSuccess = success;
        editClientMessage = message;
        if (success)
        {
            client = await ClientService.GetClientByIdAsync(ClientId, coachId);
            await Task.Delay(1200);
            showEditClientModal = false;
            editClientMessage = string.Empty;
        }
    }

    private async Task SendWhatsAppReminder()
    {
        if (client == null || string.IsNullOrEmpty(client.PhoneNumber)) return;

        string startDateStr = client.StartDate.ToString("MMM dd, yyyy");
        string endDateStr = client.EndDate.ToString("MMM dd, yyyy");

        string message = "";
        if (IsExpired)
        {
            message = $"Hey {client.Name}! I hope you are doing well.  Just a reminder that your coaching subscription (from {startDateStr} to {endDateStr}) has expired. Let me know if you are ready to renew so we can keep pushing towards your goals! ";
        }
        else if (IsExpiringSoon)
        {
            message = $"Hey {client.Name}! Great work this month.  Just a friendly heads-up that your subscription (from {startDateStr} to {endDateStr}) expires in {DaysLeft} days. Let me know if we are continuing next month so we can keep the momentum going! ";
        }

        string encodedMessage = Uri.EscapeDataString(message);
        string cleanedPhone = new string(client.PhoneNumber.Where(c => char.IsDigit(c) || c == '+').ToArray());
        string whatsappUrl = $"https://wa.me/{cleanedPhone}?text={encodedMessage}";
        
        await JS.InvokeVoidAsync("open", whatsappUrl, "_blank");
    }

    // ── Delete Confirm System ─────────────────────────────────────

    private void ConfirmDeleteDiet(int dietId, int mealCount)
    {
        deleteConfirmType = "Diet Plan";
        deleteConfirmName = $"{mealCount} Meals/Day Plan";
        deleteConfirmMessage = "This will permanently delete all meals and alternatives in this plan.";
        deleteAction = async () =>
        {
            var (s, _) = await DietService.DeleteDietAsync(ClientId, dietId, coachId);
            if (s) diets.RemoveAll(d => d.Id == dietId);
        };
        showDeleteConfirm = true;
    }

    private void ConfirmDeleteDietMeal(int dietId, int dietMealId, string mealName)
    {
        deleteConfirmType = "Meal Category";
        deleteConfirmName = mealName;
        deleteConfirmMessage = "This will permanently delete this entire meal category slot and all its ingredients from this client's plan. The template in your master dictionary library will stay completely safe.";
        deleteAction = async () =>
        {
            var (success, _, updatedDiet) = await DietService.DeleteDietMealAsync(ClientId, dietMealId, coachId);
            if (success && updatedDiet != null)
            {
                var idx = diets.FindIndex(d => d.Id == dietId);
                if (idx >= 0) diets[idx] = updatedDiet;
            }
        };
        showDeleteConfirm = true;
    }

    private void ConfirmDeleteMeal(int mealId, string mealName, int dietId, int dietMealId)
    {
        deleteConfirmType = "Meal";
        deleteConfirmName = mealName;
        deleteConfirmMessage = "This meal and all its alternatives will be permanently removed.";
        deleteAction = async () =>
        {
            var (s, _) = await DietService.DeleteMealItemAsync(mealId, coachId);
            if (s)
            {
                var diet = diets.FirstOrDefault(d => d.Id == dietId);
                var dm = diet?.DietMeals.FirstOrDefault(m => m.Id == dietMealId);
                dm?.Ingredients.RemoveAll(m => m.Id == mealId);
            }
        };
        showDeleteConfirm = true;
    }

    private void ConfirmDeleteAlternative(int altId, string altName, int mealId, int dietMealId, int dietId)
    {
        deleteConfirmType = "Alternative";
        deleteConfirmName = altName;
        deleteConfirmMessage = "This alternative will be permanently removed from the meal.";
        deleteAction = async () =>
        {
            var (s, _) = await AlternativeService.DeleteAlternativeAsync(altId, coachId);
            if (s)
            {
                var diet = diets.FirstOrDefault(d => d.Id == dietId);
                var dm = diet?.DietMeals.FirstOrDefault(m => m.Id == dietMealId);
                var meal = dm?.Ingredients.FirstOrDefault(m => m.Id == mealId);
                meal?.Alternatives.RemoveAll(a => a.Id == altId);
            }
        };
        showDeleteConfirm = true;
    }

    private void ConfirmDeleteWorkout(int workoutId, string splitName)
    {
        deleteConfirmType = "Workout Plan";
        deleteConfirmName = splitName;
        deleteConfirmMessage = "This will permanently delete all days and exercises in this plan.";
        deleteAction = async () =>
        {
            var (s, _) = await WorkoutService.DeleteWorkoutAsync(ClientId, workoutId, coachId);
            if (s) workouts.RemoveAll(w => w.Id == workoutId);
        };
        showDeleteConfirm = true;
    }

    private void ConfirmDeleteExercise(int exerciseId, string exerciseName, int workoutId, int dayId)
    {
        deleteConfirmType = "Exercise";
        deleteConfirmName = exerciseName;
        deleteConfirmMessage = "This exercise will be permanently removed from the workout day.";
        deleteAction = async () =>
        {
            var (s, _) = await WorkoutService.DeleteExerciseAsync(exerciseId, coachId);
            if (s)
            {
                var day = workouts.FirstOrDefault(w => w.Id == workoutId)
                                  ?.WorkoutDays.FirstOrDefault(d => d.Id == dayId);
                day?.Exercises.RemoveAll(e => e.Id == exerciseId);
            }
        };
        showDeleteConfirm = true;
    }

    private void ConfirmDeleteCardio(int workoutId, int dayId, string cardioType)
    {
        deleteConfirmType = "Cardio";
        deleteConfirmName = cardioType;
        deleteConfirmMessage = "The cardio session will be removed from this workout day.";
        deleteAction = async () =>
        {
            var (s, _) = await WorkoutService.DeleteCardioAsync(ClientId, workoutId, dayId, coachId);
            if (s)
            {
                var day = workouts.FirstOrDefault(w => w.Id == workoutId)
                                  ?.WorkoutDays.FirstOrDefault(d => d.Id == dayId);
                if (day != null) day.Cardio = null;
            }
        };
        showDeleteConfirm = true;
    }

    private async Task ExecuteDelete()
    {
        if (deleteAction != null) await deleteAction();
        showDeleteConfirm = false;
        deleteAction = null;
    }

    // ── Diet ──────────────────────────────────────────────────────

    private async Task CreateDiet()
    {
        var diet = await DietService.CreateDietAsync(ClientId, newDiet, coachId);
        diets.Add(diet);
        expandedDietMonths.Add(diets.Count);
        newDiet = new();
        showDietModal = false;
    }

    private void OpenEditDietModal(DietResponseDto diet)
    {
        editingDietId = diet.Id;
        editDietCurrentCount = diet.DietMeals.Count;
        editDietMealCount = diet.NumberOfMeals;
        showEditDietModal = true;
    }

    private async Task UpdateDietMealCount()
    {
        if (editDietMealCount < editDietCurrentCount) { editDietMealCount = editDietCurrentCount; return; }
        var idx = diets.FindIndex(d => d.Id == editingDietId);
        if (idx >= 0)
        {
            var d = diets[idx];
            diets[idx] = new DietResponseDto(d.Id, editDietMealCount, d.CreatedAt, d.Instructions, d.DietMeals);
        }
        showEditDietModal = false;
    }

    private void OpenEditDietNotesModal(DietResponseDto diet)
    {
        editDietNotesId = diet.Id;
        editDietInstructions = diet.Instructions;
        showEditDietNotesModal = true;
    }

    private async Task SaveDietNotes()
    {
        var updated = await DietService.UpdateDietInstructionsAsync(ClientId, editDietNotesId, editDietInstructions, coachId);
        if (updated != null)
        {
            var idx = diets.FindIndex(d => d.Id == editDietNotesId);
            if (idx >= 0) diets[idx] = updated;
        }
        showEditDietNotesModal = false;
    }

    private string newDietMealName = "";
    private string? newDietMealInstruction = null;
    private string? newDietMealLink = null;
    private List<CreateMealItemDto>? newDietMealInitialItems = null;
    private int? newDietMealParentId = null;
    private bool showDietMealModal = false;

    private void OpenAddDietMealModal(int dietId)
    {
        activeDietId = dietId;
        newDietMealName = "";
        newDietMealInstruction = null;
        newDietMealLink = null;
        newDietMealInitialItems = null;
        newDietMealParentId = null;
        showDietMealModal = true;
    }

    private void OpenAlternativeDietMealModal(int dietId, int parentId)
    {
        activeDietId = dietId;
        newDietMealName = "";
        newDietMealInstruction = null;
        newDietMealLink = null;
        newDietMealInitialItems = null;
        newDietMealParentId = parentId;
        showDietMealModal = true;
    }

    private async Task SaveDietMeal()
    {
        if (isSavingMeal) return;
        if (string.IsNullOrWhiteSpace(newDietMealName)) return;

        isSavingMeal = true;
        try
        {
            var result = await DietService.AddDietMealAsync(
                ClientId, activeDietId, newDietMealName, newDietMealInstruction, newDietMealParentId, coachId, newDietMealLink, newDietMealInitialItems);
            if (result != null)
            {
                var idx = diets.FindIndex(d => d.Id == activeDietId);
                if (idx >= 0) diets[idx] = result;
            }
            showDietMealModal = false;
        }
        finally
        {
            isSavingMeal = false;
        }
    }

    private bool showEditDietMealModal = false;
    private int editDietMealId;
    private string editDietMealName = "";
    private string? editDietMealInstruction;
    private string? editDietMealLink;

    private void OpenEditDietMealModal(int dietId, DietMealResponseDto dietMeal)
    {
        activeDietId = dietId;
        editDietMealId = dietMeal.Id;
        editDietMealName = dietMeal.Name;
        editDietMealInstruction = dietMeal.Instruction;
        editDietMealLink = dietMeal.Link;
        showEditDietMealModal = true;
    }

    private async Task SaveEditDietMeal()
    {
        if (string.IsNullOrWhiteSpace(editDietMealName)) return;
        var result = await DietService.UpdateDietMealAsync(
            ClientId, editDietMealId, editDietMealName, editDietMealInstruction, editDietMealLink, coachId);
        if (result != null)
        {
            var idx = diets.FindIndex(d => d.Id == activeDietId);
            if (idx >= 0) diets[idx] = result;
        }
        showEditDietMealModal = false;
    }

    private int activeDietMealId;

    private double originalQuantity;
    private int originalProtein;
    private int originalCarbs;
    private int originalFats;
    private int originalCalories;

    private void OpenAddIngredientModal(int dietId, int dietMealId)
    {
        activeDietId = dietId;
        activeDietMealId = dietMealId;
        editMealId = 0;
        newMeal = new();
        
        // Reset baseline
        originalQuantity = 1;
        originalProtein = 0;
        originalCarbs = 0;
        originalFats = 0;
        originalCalories = 0;
        
        showMealModal = true;
    }

    private void OpenEditMealModal(int dietId, int dietMealId, MealItemResponseDto meal)
    {
        activeDietId = dietId;
        activeDietMealId = dietMealId;
        editMealId = meal.Id;
        newMeal = new CreateMealItemDto
        {
            MealName = meal.MealName,
            Quantity = meal.Quantity,
            Unit = meal.Unit,
            Protein = meal.Protein,
            Carbs = meal.Carbs,
            Fats = meal.Fats,
            Calories = meal.Calories
        };

        // Capture original baseline values
        originalQuantity = meal.Quantity > 0 ? meal.Quantity : 1;
        originalProtein = meal.Protein;
        originalCarbs = meal.Carbs;
        originalFats = meal.Fats;
        originalCalories = meal.Calories;

        showMealModal = true;
    }

    private async Task SaveMeal()
    {
        if (editMealId > 0)
        {
            var (success, _) = await DietService.UpdateMealItemAsync(editMealId, newMeal, coachId);
            if (success)
            {
                var diet = diets.FirstOrDefault(d => d.Id == activeDietId);
                var dm = diet?.DietMeals.FirstOrDefault(m => m.Id == activeDietMealId);
                if (dm != null)
                {
                    var idx = dm.Ingredients.FindIndex(m => m.Id == editMealId);
                    if (idx >= 0)
                        dm.Ingredients[idx] = new MealItemResponseDto(
                            editMealId, newMeal.MealName, 
                            newMeal.Quantity, newMeal.Unit,
                            newMeal.Protein, newMeal.Carbs, newMeal.Fats,
                            newMeal.Calories,
                            dm.Ingredients[idx].Alternatives);
                }
            }
        }
        else
        {
            var result = await DietService.AddMealItemAsync(ClientId, activeDietMealId, newMeal, coachId);
            if (result != null)
            {
                var idx = diets.FindIndex(d => d.Id == activeDietId);
                if (idx >= 0) diets[idx] = result;
            }
        }
        newMeal = new();
        editMealId = 0;
        showMealModal = false;
    }

    private void OpenAddAlternativeModal(int mealId, int dietMealId, int dietId)
    {
        activeMealId = mealId;
        activeDietMealId = dietMealId;
        activeDietId = dietId;
        editAlternativeId = 0;
        newAlternative = new();

        // Reset baseline
        originalQuantity = 1;
        originalProtein = 0;
        originalCarbs = 0;
        originalFats = 0;
        originalCalories = 0;

        showAlternativeModal = true;
    }

    private void OpenEditAlternativeModal(int mealId, int dietMealId, int dietId, AlternativeResponeDto alt)
    {
        activeMealId = mealId;
        activeDietMealId = dietMealId;
        activeDietId = dietId;
        editAlternativeId = alt.Id;
        newAlternative = new CreateAlternativeDto
        {
            MealName = alt.MealName,
            Description = alt.Description,
            Quantity = alt.Quantity,
            Unit = alt.Unit,
            Protein = alt.Protein,
            Carbs = alt.Carbs,
            Fats = alt.Fats,
            Calories = alt.Calories
        };

        // Capture original baseline values
        originalQuantity = alt.Quantity > 0 ? alt.Quantity : 1;
        originalProtein = alt.Protein;
        originalCarbs = alt.Carbs;
        originalFats = alt.Fats;
        originalCalories = alt.Calories;

        showAlternativeModal = true;
    }

    private async Task SaveAlternative()
    {
        if (editAlternativeId > 0)
        {
            var (success, _) = await AlternativeService.UpdateAlternativeAsync(editAlternativeId, newAlternative, coachId);
            if (success)
            {
                var diet = diets.FirstOrDefault(d => d.Id == activeDietId);
                var dm = diet?.DietMeals.FirstOrDefault(m => m.Id == activeDietMealId);
                var meal = dm?.Ingredients.FirstOrDefault(m => m.Id == activeMealId);
                if (meal != null)
                {
                    var idx = meal.Alternatives.FindIndex(a => a.Id == editAlternativeId);
                    if (idx >= 0)
                        meal.Alternatives[idx] = new AlternativeResponeDto(
                            editAlternativeId, newAlternative.MealName, newAlternative.Description,
                            newAlternative.Quantity, newAlternative.Unit,
                            newAlternative.Protein, newAlternative.Carbs, newAlternative.Fats,
                            newAlternative.Calories);
                }
            }
        }
        else
        {
            var result = await AlternativeService.AddAlternativeAsync(activeMealId, newAlternative, coachId);
            var diet = diets.FirstOrDefault(d => d.Id == activeDietId);
            var dm = diet?.DietMeals.FirstOrDefault(m => m.Id == activeDietMealId);
            var meal = dm?.Ingredients.FirstOrDefault(m => m.Id == activeMealId);
            meal?.Alternatives.Add(result);
        }
        newAlternative = new();
        editAlternativeId = 0;
        showAlternativeModal = false;
    }

    // ── Dynamic Macro Scaling Callbacks ────────────────────────────

    private void OnQuantityChanged()
    {
        if (originalQuantity > 0 && newMeal.Quantity > 0)
        {
            if (originalProtein == 0 && originalCarbs == 0 && originalFats == 0 && originalCalories == 0)
            {
                // First-time manual baseline setting
                originalQuantity = newMeal.Quantity;
                originalProtein = newMeal.Protein;
                originalCarbs = newMeal.Carbs;
                originalFats = newMeal.Fats;
                originalCalories = newMeal.Calories;
            }
            else
            {
                double ratio = newMeal.Quantity / originalQuantity;
                newMeal.Protein = (int)Math.Round(originalProtein * ratio);
                newMeal.Carbs = (int)Math.Round(originalCarbs * ratio);
                newMeal.Fats = (int)Math.Round(originalFats * ratio);
                newMeal.Calories = (int)Math.Round(originalCalories * ratio);
            }
        }
    }

    private void OnAlternativeQuantityChanged()
    {
        if (originalQuantity > 0 && newAlternative.Quantity > 0)
        {
            if (originalProtein == 0 && originalCarbs == 0 && originalFats == 0 && originalCalories == 0)
            {
                // First-time manual baseline setting
                originalQuantity = newAlternative.Quantity;
                originalProtein = newAlternative.Protein;
                originalCarbs = newAlternative.Carbs;
                originalFats = newAlternative.Fats;
                originalCalories = newAlternative.Calories;
            }
            else
            {
                double ratio = newAlternative.Quantity / originalQuantity;
                newAlternative.Protein = (int)Math.Round(originalProtein * ratio);
                newAlternative.Carbs = (int)Math.Round(originalCarbs * ratio);
                newAlternative.Fats = (int)Math.Round(originalFats * ratio);
                newAlternative.Calories = (int)Math.Round(originalCalories * ratio);
            }
        }
    }

    private void OnProteinChanged() { originalProtein = newMeal.Protein; originalQuantity = newMeal.Quantity > 0 ? newMeal.Quantity : 1; }
    private void OnCarbsChanged() { originalCarbs = newMeal.Carbs; originalQuantity = newMeal.Quantity > 0 ? newMeal.Quantity : 1; }
    private void OnFatsChanged() { originalFats = newMeal.Fats; originalQuantity = newMeal.Quantity > 0 ? newMeal.Quantity : 1; }
    private void OnCaloriesChanged() { originalCalories = newMeal.Calories; originalQuantity = newMeal.Quantity > 0 ? newMeal.Quantity : 1; }

    private void OnAltProteinChanged() { originalProtein = newAlternative.Protein; originalQuantity = newAlternative.Quantity > 0 ? newAlternative.Quantity : 1; }
    private void OnAltCarbsChanged() { originalCarbs = newAlternative.Carbs; originalQuantity = newAlternative.Quantity > 0 ? newAlternative.Quantity : 1; }
    private void OnAltFatsChanged() { originalFats = newAlternative.Fats; originalQuantity = newAlternative.Quantity > 0 ? newAlternative.Quantity : 1; }
    private void OnAltCaloriesChanged() { originalCalories = newAlternative.Calories; originalQuantity = newAlternative.Quantity > 0 ? newAlternative.Quantity : 1; }

    // ── Workout ───────────────────────────────────────────────────

    private async Task CreateWorkout()
    {
        var workout = await WorkoutService.CreateWorkoutAsync(ClientId, newWorkout, coachId);
        workouts.Add(workout);
        expandedWorkoutMonths.Add(workouts.Count);
        newWorkout = new();
        showWorkoutModal = false;
    }

    private void OpenEditWorkoutModal(WorkoutResponseDto workout)
    {
        editingWorkoutId = workout.Id;
        editWorkoutCurrentCount = workout.WorkoutDays.Count;
        editWorkoutDayCount = workout.NumberOfWorkoutDays;
        showEditWorkoutModal = true;
    }

    private async Task UpdateWorkoutDayCount()
    {
        if (editWorkoutDayCount < editWorkoutCurrentCount) { editWorkoutDayCount = editWorkoutCurrentCount; return; }
        var idx = workouts.FindIndex(w => w.Id == editingWorkoutId);
        if (idx >= 0)
        {
            var w = workouts[idx];
            workouts[idx] = new WorkoutResponseDto(w.Id, w.SplitName, editWorkoutDayCount, w.CreatedAt, w.WorkoutDays);
        }
        showEditWorkoutModal = false;
    }

    private void OpenAddDayModal(int workoutId)
    {
        activeWorkoutId = workoutId;
        newDay = new();
        dayValidationError = null;
        var used = UsedDaysForActiveWorkout;
        var firstAvailable = Enumerable.Range(0, 7)
            .Select(i => (Days)i)
            .FirstOrDefault(d => !used.Contains(d));
        newDayEnum = (int)firstAvailable;
        showDayModal = true;
    }

    private async Task AddWorkoutDay()
    {
        if (string.IsNullOrWhiteSpace(newDay.DayName))
        {
            dayValidationError = "Day Name is required.";
            return;
        }
        dayValidationError = null;

        newDay.Day = (Days)newDayEnum;
        var day = await WorkoutService.AddWorkoutDayAsync(ClientId, activeWorkoutId, newDay, coachId);
        if (day != null)
        {
            var workout = workouts.FirstOrDefault(w => w.Id == activeWorkoutId);
            if (workout != null)
            {
                workout.WorkoutDays.Add(day);
                workout.WorkoutDays = workout.WorkoutDays.OrderBy(d => d.Day).ToList();
            }
        }
        newDay = new();
        showDayModal = false;
    }

    private void OpenEditDayModal(int workoutId, WorkoutDayResponseDto day)
    {
        activeWorkoutId = workoutId;
        editingDayId = day.Id;
        editDayName = day.DayName;
        editDayEnum = (int)day.Day;
        editDayValidationError = null;
        showEditDayModal = true;
    }

    private async Task UpdateWorkoutDay()
    {
        if (string.IsNullOrWhiteSpace(editDayName))
        {
            editDayValidationError = "Day Name is required.";
            return;
        }
        editDayValidationError = null;

        var targetDayOfWeek = (Days)editDayEnum;
        var (success, msg) = await WorkoutService.UpdateWorkoutDayAsync(ClientId, activeWorkoutId, editingDayId, editDayName, targetDayOfWeek, coachId);
        if (success)
        {
            var workout = workouts.FirstOrDefault(w => w.Id == activeWorkoutId);
            if (workout != null)
            {
                var dayIdx = workout.WorkoutDays.FindIndex(d => d.Id == editingDayId);
                if (dayIdx >= 0)
                {
                    var oldDay = workout.WorkoutDays[dayIdx];
                    workout.WorkoutDays[dayIdx] = new WorkoutDayResponseDto(
                        oldDay.Id,
                        editDayName,
                        targetDayOfWeek,
                        oldDay.Exercises,
                        oldDay.Cardio,
                        oldDay.Notes,
                        oldDay.WarmUpName,
                        oldDay.WarmUpLink
                    );
                    workout.WorkoutDays = workout.WorkoutDays.OrderBy(d => d.Day).ToList();
                }
            }
            showEditDayModal = false;
        }
        else
        {
            editDayValidationError = msg;
        }
    }

    private void ConfirmDeleteDay(int workoutId, WorkoutDayResponseDto day)
    {
        deleteConfirmType = "Workout Day";
        deleteConfirmName = day.DayName;
        deleteConfirmMessage = $"This will permanently delete this day ({day.Day.ToString()}: {day.DayName}) and all of its exercises and notes.";
        deleteAction = async () =>
        {
            var (s, _) = await WorkoutService.DeleteWorkoutDayAsync(ClientId, workoutId, day.Id, coachId);
            if (s)
            {
                var workout = workouts.FirstOrDefault(w => w.Id == workoutId);
                workout?.WorkoutDays.RemoveAll(d => d.Id == day.Id);
            }
        };
        showDeleteConfirm = true;
    }

    private void OpenAddExerciseModal(int workoutId, int dayId)
    {
        activeWorkoutId = workoutId;
        activeDayId = dayId;
        editExerciseId = 0;
        newExercise = new();
        exerciseValidationError = null;
        showExerciseModal = true;
    }

    private void OpenEditExerciseModal(int workoutId, int dayId, ExerciseResponseDto ex)
    {
        activeWorkoutId = workoutId;
        activeDayId = dayId;
        editExerciseId = ex.Id;
        newExercise = new CreateExerciseDto
        {
            ExerciseName = ex.ExerciseName,
            Sets = ex.Sets,
            Reps = ex.Reps,
            RestTime = ex.RestTime,
            ExerciseLink = ex.ExerciseLink
        };
        exerciseValidationError = null;
        showExerciseModal = true;
    }

    private async Task SaveExercise()
    {
        if (string.IsNullOrWhiteSpace(newExercise.ExerciseName))
        {
            exerciseValidationError = "Exercise name is required.";
            return;
        }
        if (string.IsNullOrWhiteSpace(newExercise.Sets))
        {
            exerciseValidationError = "Sets is required.";
            return;
        }
        if (string.IsNullOrWhiteSpace(newExercise.Reps))
        {
            exerciseValidationError = "Reps is required.";
            return;
        }
        if (string.IsNullOrWhiteSpace(newExercise.RestTime))
        {
            exerciseValidationError = "Rest time is required.";
            return;
        }

        exerciseValidationError = null;

        if (editExerciseId > 0)
        {
            var (success, _) = await WorkoutService.UpdateExerciseAsync(editExerciseId, newExercise, coachId);
            if (success)
            {
                var day = workouts.FirstOrDefault(w => w.Id == activeWorkoutId)
                                  ?.WorkoutDays.FirstOrDefault(d => d.Id == activeDayId);
                if (day != null)
                {
                    var idx = day.Exercises.FindIndex(e => e.Id == editExerciseId);
                    if (idx >= 0)
                        day.Exercises[idx] = new ExerciseResponseDto(
                            editExerciseId, newExercise.ExerciseName, newExercise.Sets,
                            newExercise.Reps, newExercise.RestTime, newExercise.ExerciseLink);
                }
            }
        }
        else
        {
            var ex = await WorkoutService.AddExerciseAsync(ClientId, activeWorkoutId, activeDayId, newExercise, coachId);
            if (ex != null)
                workouts.FirstOrDefault(w => w.Id == activeWorkoutId)
                        ?.WorkoutDays.FirstOrDefault(d => d.Id == activeDayId)
                        ?.Exercises.Add(ex);
        }
        newExercise = new();
        editExerciseId = 0;
        showExerciseModal = false;
    }

    // ── Cardio ────────────────────────────────────────────────────

    private void OpenAddCardioModal(int workoutId, int dayId)
    {
        activeWorkoutId = workoutId;
        activeDayId = dayId;
        newCardio = new();
        newCardioIntensity = 1;
        showCardioModal = true;
    }

    private async Task AddCardio()
    {
        newCardio.Intensity = (CardioIntensity)newCardioIntensity;
        var cardio = await WorkoutService.AddCardioAsync(ClientId, activeWorkoutId, activeDayId, newCardio, coachId);
        if (cardio != null)
        {
            var day = workouts.FirstOrDefault(w => w.Id == activeWorkoutId)
                              ?.WorkoutDays.FirstOrDefault(d => d.Id == activeDayId);
            if (day != null) day.Cardio = cardio;
        }
        newCardio = new();
        showCardioModal = false;
    }

    private void OpenEditCardioModal(int workoutId, int dayId, CardioResponseDto cardio)
    {
        activeWorkoutId = workoutId;
        activeDayId = dayId;
        editCardio = new CreateCardioDto
        {
            CardioType = cardio.CardioType,
            DurationMinutes = cardio.DurationMinutes,
            Intensity = cardio.Intensity,
            Notes = cardio.Notes
        };
        editCardioIntensity = (int)cardio.Intensity;
        showEditCardioModal = true;
    }

    private async Task SaveEditCardio()
    {
        editCardio.Intensity = (CardioIntensity)editCardioIntensity;
        var (success, _) = await WorkoutService.UpdateCardioAsync(
            ClientId, activeWorkoutId, activeDayId, editCardio, coachId);
        if (success)
        {
            var day = workouts.FirstOrDefault(w => w.Id == activeWorkoutId)
                              ?.WorkoutDays.FirstOrDefault(d => d.Id == activeDayId);
            if (day != null)
                day.Cardio = new CardioResponseDto(
                    day.Cardio!.Id,
                    editCardio.CardioType,
                    editCardio.DurationMinutes,
                    editCardio.Intensity,
                    editCardio.Notes);
        }
        showEditCardioModal = false;
    }

    private void OpenEditWorkoutDayNotesModal(int workoutId, WorkoutDayResponseDto day)
    {
        editWorkoutDayNotesWorkoutId = workoutId;
        editWorkoutDayNotesDayId = day.Id;
        editWorkoutDayNotesText = day.Notes;
        showEditWorkoutDayNotesModal = true;
    }

    private async Task SaveWorkoutDayNotes()
    {
        var updated = await WorkoutService.UpdateWorkoutDayNotesAsync(
            ClientId, editWorkoutDayNotesWorkoutId, editWorkoutDayNotesDayId, editWorkoutDayNotesText, coachId);
        if (updated != null)
        {
            var day = workouts.FirstOrDefault(w => w.Id == editWorkoutDayNotesWorkoutId)
                              ?.WorkoutDays.FirstOrDefault(d => d.Id == editWorkoutDayNotesDayId);
            if (day != null)
            {
                day.Notes = updated.Notes;
            }
        }
        showEditWorkoutDayNotesModal = false;
    }

    private async Task ConfirmDeleteWorkoutDayNotes(int workoutId, int dayId)
    {
        var updated = await WorkoutService.UpdateWorkoutDayNotesAsync(
            ClientId, workoutId, dayId, null, coachId);
        if (updated != null)
        {
            var day = workouts.FirstOrDefault(w => w.Id == workoutId)
                              ?.WorkoutDays.FirstOrDefault(d => d.Id == dayId);
            if (day != null)
            {
                day.Notes = null;
            }
        }
    }

    // ── Edit Workout Day Warm-Up ──────────────────────────────────
    private void OpenEditWarmUpModal(int workoutId, WorkoutDayResponseDto day)
    {
        editWarmUpWorkoutId = workoutId;
        editWarmUpDayId = day.Id;
        editWarmUpName = day.WarmUpName;
        editWarmUpLink = day.WarmUpLink;
        showEditWarmUpModal = true;
    }

    private void OnWarmUpNameChanged()
    {
        var match = warmUpDict.FirstOrDefault(w => string.Equals(w.WarmUpName, editWarmUpName, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            editWarmUpLink = match.WarmUpLink;
        }
    }

    private async Task SaveWarmUp()
    {
        var (success, msg) = await WorkoutService.UpdateWarmUpAsync(
            ClientId, editWarmUpWorkoutId, editWarmUpDayId, editWarmUpName, editWarmUpLink, coachId);
        if (success)
        {
            var day = workouts.FirstOrDefault(w => w.Id == editWarmUpWorkoutId)
                              ?.WorkoutDays.FirstOrDefault(d => d.Id == editWarmUpDayId);
            if (day != null)
            {
                day.WarmUpName = editWarmUpName;
                day.WarmUpLink = editWarmUpLink;
            }
        }
        showEditWarmUpModal = false;
    }

    private async Task RemoveWarmUp(int workoutId, int dayId)
    {
        var (success, msg) = await WorkoutService.UpdateWarmUpAsync(
            ClientId, workoutId, dayId, null, null, coachId);
        if (success)
        {
            var day = workouts.FirstOrDefault(w => w.Id == workoutId)
                              ?.WorkoutDays.FirstOrDefault(d => d.Id == dayId);
            if (day != null)
            {
                day.WarmUpName = null;
                day.WarmUpLink = null;
            }
        }
    }

    // ── Subscription ──────────────────────────────────────────────

    private async Task RenewSubscription()
    {
        if (client == null) return;
        isRenewing = true;
        renewMessage = string.Empty;
        var (success, message) = await ClientService.RenewSubscriptionAsync(ClientId, renewMonths, coachId);
        isRenewing = false;
        renewSuccess = success;
        renewMessage = message;
        if (success)
        {
            client = await ClientService.GetClientByIdAsync(ClientId, coachId);
            await Task.Delay(1500);
            showRenewModal = false;
            renewMessage = string.Empty;
        }
    }

    private void OnMealNameChanged()
    {
    }

    private void OnAlternativeNameChanged()
    {
    }

    private void OnDietMealNameChanged()
    {
        var match = mealDict.FirstOrDefault(m => string.Equals(m.MealName, newDietMealName, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            newDietMealLink = match.Link;
            newDietMealInstruction = match.Instruction;
            if (!string.IsNullOrEmpty(match.IngredientsJson))
            {
                try
                {
                    newDietMealInitialItems = System.Text.Json.JsonSerializer.Deserialize<List<CreateMealItemDto>>(
                        match.IngredientsJson,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch
                {
                    newDietMealInitialItems = null;
                }
            }
            else
            {
                newDietMealInitialItems = null;
            }
        }
        else
        {
            newDietMealLink = null;
            newDietMealInstruction = null;
            newDietMealInitialItems = null;
        }
    }

    private void OnExerciseNameChanged()
    {
        var match = exerciseDict.FirstOrDefault(e => string.Equals(e.ExerciseName, newExercise.ExerciseName, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            newExercise.ExerciseLink = match.ExcerciseLink;
        }
    }

    // ── Templates Logic ───────────────────────────────────────────

    private void OpenSaveDietTemplateModal(int dietId)
    {
        savingTemplateDietId = dietId;
        dietTemplateName = "";
        showSaveDietTemplateModal = true;
    }

    private async Task SaveDietAsTemplate()
    {
        if (string.IsNullOrWhiteSpace(dietTemplateName)) return;
        var template = await TemplateService.SaveDietAsTemplateAsync(savingTemplateDietId, dietTemplateName, coachId);
        dietTemplates = await TemplateService.GetDietTemplatesAsync(coachId);
        showSaveDietTemplateModal = false;
        await JS.InvokeVoidAsync("alert", $"Diet template '{template.Name}' saved successfully!");
    }

    private void OpenApplyDietTemplateModal()
    {
        selectedDietTemplateId = 0;
        showApplyDietTemplateModal = true;
    }

    private async Task ApplyDietTemplate()
    {
        if (selectedDietTemplateId == 0) return;
        isLoading = true;
        showApplyDietTemplateModal = false;
        var success = await TemplateService.CloneDietTemplateAsync(selectedDietTemplateId, ClientId, coachId);
        if (success)
        {
            diets = await DietService.GetDietsAsync(ClientId, coachId);
            expandedDietMonths.Add(diets.Count);
        }
        isLoading = false;
    }

    private void OpenSaveWorkoutTemplateModal(int workoutId)
    {
        savingTemplateWorkoutId = workoutId;
        workoutTemplateName = "";
        showSaveWorkoutTemplateModal = true;
    }

    private async Task SaveWorkoutAsTemplate()
    {
        if (string.IsNullOrWhiteSpace(workoutTemplateName)) return;
        var template = await TemplateService.SaveWorkoutAsTemplateAsync(savingTemplateWorkoutId, workoutTemplateName, coachId);
        workoutTemplates = await TemplateService.GetWorkoutTemplatesAsync(coachId);
        showSaveWorkoutTemplateModal = false;
        await JS.InvokeVoidAsync("alert", $"Workout template '{template.Name}' saved successfully!");
    }

    private void OpenApplyWorkoutTemplateModal()
    {
        selectedWorkoutTemplateId = 0;
        showApplyWorkoutTemplateModal = true;
    }

    private async Task ApplyWorkoutTemplate()
    {
        if (selectedWorkoutTemplateId == 0) return;
        isLoading = true;
        showApplyWorkoutTemplateModal = false;
        var success = await TemplateService.CloneWorkoutTemplateAsync(selectedWorkoutTemplateId, ClientId, coachId);
        if (success)
        {
            workouts = await WorkoutService.GetWorkoutsAsync(ClientId, coachId);
            expandedWorkoutMonths.Add(workouts.Count);
        }
        isLoading = false;
    }
}
