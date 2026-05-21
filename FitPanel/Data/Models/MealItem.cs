using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitPanel.Data.Models;

    public class MealItem
    {
        public int Id { get; set; }

        public string MealName { get; set; }
        
        public int Protein { get; set; }
        public int Carbs { get; set; }

        public int Fats { get; set; }

        public int Calories { get; set; }


        public double Quantity { get; set; } = 1;
        public string? Unit { get; set; } // e.g., "pieces", "g", "ml"

        // Foreign Key to DietMeal
        public int DietMealId { get; set; }   // FK
        public DietMeal DietMeal { get; set; }    // Navigation

        public ICollection<AlternativeItem> AlternativeItems { get; set; }
        

       
    }
