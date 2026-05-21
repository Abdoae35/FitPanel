using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitPanel.Data.Models
{
    public class AlternativeItem
    {
         public int Id { get; set; }

        public string MealName { get; set; }
        public string Description { get; set; }
        public int Protein { get; set; }
        public int Carbs { get; set; }

        public int Fats { get; set; }
        public int Calories { get; set; }

        public double Quantity { get; set; }
        public string? Unit { get; set; }

        // Foreign Key to MealItem
        public int MealItemId { get; set; }   // FK
        public MealItem MealItem { get; set; }    // Navigation



    }
}