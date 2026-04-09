using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitPanel.DTOs.Workout;
public class CreateWorkoutDayDto
{
    public string DayName { get; set; } = string.Empty;
    public Days Day { get; set; } = Days.Day1;
}
