using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitPanel.Data.Models;

    public class Excercise
    {
        public int Id { get; set; }
        public string ExerciseName { get; set; }
        public string? ExcerciseLink { get; set; }
        public string RestTime  { get; set; }
        public string Reps  { get; set; }
        public string Sets  { get; set; }

        // Foreign Key to WorkOut
        public int WorkOutDayId { get; set; }   // FK
        public WorkOutDay WorkOutDay { get; set; }  // Navigation
    }
