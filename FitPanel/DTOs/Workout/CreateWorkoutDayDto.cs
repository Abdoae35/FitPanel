using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitPanel.DTOs.Workout;

    public record CreateWorkoutDayDto(
    string DayName,
    Days Day
);
