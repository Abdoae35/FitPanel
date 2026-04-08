using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitPanel.DTOs.Workout;

   public record ExerciseResponseDto(
    int Id,
    string ExerciseName,
    string Sets,
    string Reps,
    string RestTime,
    string? ExerciseLink
);
