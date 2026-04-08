using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitPanel.Data.Models;

    public class MealItem
    {
        public int Id { get; set; }

        public string MealName { get; set; }
        public string Description { get; set; }
        public string? Link { get; set; }
        public int Protein { get; set; }
        public int Carbs { get; set; }

        public int Fats { get; set; }

        public int Calories { get; set; }


        // Foreign Key to Diet

        public int DietId { get; set; }   // FK
        public Diet Diet { get; set; }    // Navigation
        

       
    }
