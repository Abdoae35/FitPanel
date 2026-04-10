using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitPanel.Data.Models;

    public class Diet
    {
        
        public int Id { get; set; }
       public DateTime CreatedAt { get; set; }
        public int NumberOfMeals { get; set; }
        public string? Instructions { get; set; }


        // Foreign Key to Client
        public int ClientId { get; set; }   // FK
        public Client Client { get; set; }  // Navigation

        //Apply one to many relationship with Meal
        public ICollection<MealItem> MealItems { get; set; } = new List<MealItem>();

        
    }
