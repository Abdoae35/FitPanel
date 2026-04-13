using FitPanel.Data;
using FitPanel.Data.Models;
using FitPanel.DTOs.Workout;
using Microsoft.EntityFrameworkCore;

namespace FitPanel.Services;

public class WorkoutService : IWorkoutService
{
    private readonly FitPanelDbContext _db;

    public WorkoutService(FitPanelDbContext db)
    {
        _db = db;
    }

    // ─── Workout ────────────────────────────────────────────────

    public async Task<WorkoutResponseDto> CreateWorkoutAsync(
        int clientId, CreateWorkoutDto dto, string coachId)
    {
        var client = await _db.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.CoachId == coachId)
            ?? throw new UnauthorizedAccessException("Client not found.");

        var workout = new WorkOut
        {
            ClientId = clientId,
            SplitName = dto.SplitName,
            NumberOfWorkOutDays = dto.NumberOfWorkoutDays,
            CreatedAt = DateTime.UtcNow
        };

        _db.WorkOuts.Add(workout);
        await _db.SaveChangesAsync();

        return MapWorkoutToDto(workout, new List<WorkOutDay>());
    }

    public async Task<List<WorkoutResponseDto>> GetWorkoutsAsync(
        int clientId, string coachId)
    {
        var client = await _db.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.CoachId == coachId)
            ?? throw new UnauthorizedAccessException("Client not found.");

        return await _db.WorkOuts
            .Where(w => w.ClientId == clientId)
            .Include(w => w.WorkOutDays)
                .ThenInclude(d => d.ExcerciseItems)
            .Include(w => w.WorkOutDays)
                .ThenInclude(d => d.Cardio)
            .Select(w => new WorkoutResponseDto(
                w.Id,
                w.SplitName,
                w.NumberOfWorkOutDays,
                w.CreatedAt,
                w.WorkOutDays.Select(d => new WorkoutDayResponseDto(
                    d.Id,
                    d.DayName,
                    d.Day,
                    d.ExcerciseItems.Select(e => new ExerciseResponseDto(
                        e.Id,
                        e.ExerciseName,
                        e.Sets,
                        e.Reps,
                        e.RestTime,
                        e.ExcerciseLink
                    )).ToList(),
                    d.Cardio == null ? null : new CardioResponseDto(
                        d.Cardio.Id,
                        d.Cardio.CardioType,
                        d.Cardio.DurationMinutes,
                        d.Cardio.Intensity,
                        d.Cardio.Notes)
                )).ToList()
            ))
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> DeleteWorkoutAsync(
        int clientId, int workoutId, string coachId)
    {
        var workout = await _db.WorkOuts
            .Include(w => w.Client)
            .FirstOrDefaultAsync(w => w.Id == workoutId
                && w.ClientId == clientId
                && w.Client.CoachId == coachId);

        if (workout == null) return (false, "Workout not found.");

        _db.WorkOuts.Remove(workout);
        await _db.SaveChangesAsync();
        return (true, "Workout deleted.");
    }

    // ─── Workout Day ────────────────────────────────────────────

    public async Task<WorkoutDayResponseDto?> AddWorkoutDayAsync(
        int clientId, int workoutId, CreateWorkoutDayDto dto, string coachId)
    {
        var workout = await _db.WorkOuts
            .Include(w => w.Client)
            .Include(w => w.WorkOutDays)
            .FirstOrDefaultAsync(w => w.Id == workoutId
                && w.ClientId == clientId
                && w.Client.CoachId == coachId);

        if (workout == null) return null;

        var day = new WorkOutDay
        {
            WorkOutId = workoutId,
            DayName = dto.DayName,
            Day = dto.Day
        };

        _db.WorkOutDays.Add(day);
        await _db.SaveChangesAsync();

        return new WorkoutDayResponseDto(
            day.Id,
            day.DayName,
            day.Day,
            new List<ExerciseResponseDto>(),
            null);
    }

    // ─── Exercise ───────────────────────────────────────────────

    public async Task<ExerciseResponseDto?> AddExerciseAsync(
        int clientId, int workoutId, int dayId, CreateExerciseDto dto, string coachId)
    {
        var day = await _db.WorkOutDays
            .Include(d => d.WorkOut)
                .ThenInclude(w => w.Client)
            .FirstOrDefaultAsync(d => d.Id == dayId
                && d.WorkOutId == workoutId
                && d.WorkOut.ClientId == clientId
                && d.WorkOut.Client.CoachId == coachId);

        if (day == null) return null;

        var exercise = new Excercise
        {
            WorkOutDayId = dayId,
            ExerciseName = dto.ExerciseName,
            Sets = dto.Sets,
            Reps = dto.Reps,
            RestTime = dto.RestTime,
            ExcerciseLink = dto.ExerciseLink
        };

        _db.Excercises.Add(exercise);
        await _db.SaveChangesAsync();

        return new ExerciseResponseDto(
            exercise.Id,
            exercise.ExerciseName,
            exercise.Sets,
            exercise.Reps,
            exercise.RestTime,
            exercise.ExcerciseLink);
    }

    // ─── Cardio ─────────────────────────────────────────────────

    public async Task<CardioResponseDto?> AddCardioAsync(
        int clientId, int workoutId, int dayId, CreateCardioDto dto, string coachId)
    {
        var day = await _db.WorkOutDays
            .Include(d => d.WorkOut)
                .ThenInclude(w => w.Client)
            .Include(d => d.Cardio)
            .FirstOrDefaultAsync(d => d.Id == dayId
                && d.WorkOutId == workoutId
                && d.WorkOut.ClientId == clientId
                && d.WorkOut.Client.CoachId == coachId);

        if (day == null) return null;

        // Replace existing cardio if already set
        if (day.Cardio != null)
            _db.Cardios.Remove(day.Cardio);

        var cardio = new Cardio
        {
            WorkOutDayId = dayId,
            CardioType = dto.CardioType,
            DurationMinutes = dto.DurationMinutes,
            Intensity = dto.Intensity,
            Notes = dto.Notes
        };

        _db.Cardios.Add(cardio);
        await _db.SaveChangesAsync();

        return new CardioResponseDto(
            cardio.Id,
            cardio.CardioType,
            cardio.DurationMinutes,
            cardio.Intensity,
            cardio.Notes);
    }

    public async Task<(bool Success, string Message)> DeleteCardioAsync(
        int clientId, int workoutId, int dayId, string coachId)
    {
        var day = await _db.WorkOutDays
            .Include(d => d.WorkOut)
                .ThenInclude(w => w.Client)
            .Include(d => d.Cardio)
            .FirstOrDefaultAsync(d => d.Id == dayId
                && d.WorkOutId == workoutId
                && d.WorkOut.ClientId == clientId
                && d.WorkOut.Client.CoachId == coachId);

        if (day == null) return (false, "Day not found.");
        if (day.Cardio == null) return (false, "No cardio on this day.");

        _db.Cardios.Remove(day.Cardio);
        await _db.SaveChangesAsync();
        return (true, "Cardio removed.");
    }

    // ─── Private Mapper ─────────────────────────────────────────

    private WorkoutResponseDto MapWorkoutToDto(WorkOut workout, List<WorkOutDay> days) =>
        new(
            workout.Id,
            workout.SplitName,
            workout.NumberOfWorkOutDays,
            workout.CreatedAt,
            days.Select(d => new WorkoutDayResponseDto(
                d.Id,
                d.DayName,
                d.Day,
                d.ExcerciseItems.Select(e => new ExerciseResponseDto(
                    e.Id,
                    e.ExerciseName,
                    e.Sets,
                    e.Reps,
                    e.RestTime,
                    e.ExcerciseLink
                )).ToList(),
                d.Cardio == null ? null : new CardioResponseDto(
                    d.Cardio.Id,
                    d.Cardio.CardioType,
                    d.Cardio.DurationMinutes,
                    d.Cardio.Intensity,
                    d.Cardio.Notes)
            )).ToList()
        );

   public async Task<(bool Success, string Message)> DeleteExerciseAsync(
    int exerciseId, string coachId)
{
    var exercise = await _db.Excercises
        .Include(e => e.WorkOutDay)
            .ThenInclude(d => d.WorkOut)
                .ThenInclude(w => w.Client)
        .FirstOrDefaultAsync(e => e.Id == exerciseId
            && e.WorkOutDay.WorkOut.Client.CoachId == coachId);

    if (exercise == null) return (false, "Exercise not found.");

    _db.Excercises.Remove(exercise);
    await _db.SaveChangesAsync();
    return (true, "Exercise deleted.");
}

        public async Task<(bool Success, string Message)> UpdateExerciseAsync(
            int exerciseId, CreateExerciseDto dto, string coachId)
        {
            var exercise = await _db.Excercises
                .Include(e => e.WorkOutDay)
                    .ThenInclude(d => d.WorkOut)
                        .ThenInclude(w => w.Client)
                .FirstOrDefaultAsync(e => e.Id == exerciseId
                    && e.WorkOutDay.WorkOut.Client.CoachId == coachId);

            if (exercise == null) return (false, "Exercise not found.");

            exercise.ExerciseName = dto.ExerciseName;
            exercise.Sets = dto.Sets;
            exercise.Reps = dto.Reps;
            exercise.RestTime = dto.RestTime;
            if (dto.ExerciseLink != null) exercise.ExcerciseLink = dto.ExerciseLink;

            await _db.SaveChangesAsync();
            return (true, "Exercise updated.");
        }

            public async Task<(bool Success, string Message)> UpdateCardioAsync(
                int clientId, int workoutId, int dayId, CreateCardioDto dto, string coachId)
            {
                var day = await _db.WorkOutDays
                    .Include(d => d.WorkOut)
                        .ThenInclude(w => w.Client)
                    .Include(d => d.Cardio)
                    .FirstOrDefaultAsync(d => d.Id == dayId
                        && d.WorkOutId == workoutId
                        && d.WorkOut.ClientId == clientId
                        && d.WorkOut.Client.CoachId == coachId);

                if (day == null) return (false, "Day not found.");
                if (day.Cardio == null) return (false, "No cardio on this day.");

                day.Cardio.CardioType = dto.CardioType;
                day.Cardio.DurationMinutes = dto.DurationMinutes;
                day.Cardio.Intensity = dto.Intensity;
                day.Cardio.Notes = dto.Notes;

                await _db.SaveChangesAsync();
                return (true, "Cardio updated.");
            }

}