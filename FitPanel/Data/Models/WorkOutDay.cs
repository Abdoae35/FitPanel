using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitPanel.Data.Models;

    public class WorkOutDay
    {
        public int Id { get; set; }
        public Days Day { get; set; }
        public string DayName { get; set; }
        // Foreign Key to WorkOut
        public int WorkOutId { get; set; }   // FK
        public WorkOut WorkOut { get; set; }  // Navigation

        // Optional per-day notes shown in the PDF below the exercise table
        public string? Notes { get; set; }

        // Optional warm-up set — a name + hyperlink shown at the top of the day
        public string? WarmUpName { get; set; }
        public string? WarmUpLink { get; set; }

        //Apply one to many relationship with Excercise
        public ICollection<Excercise> ExcerciseItems { get; set; } = new List<Excercise>();

        // Optional cardio — null means no cardio this day
        public Cardio? Cardio { get; set; }
    }
