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
        

        //Apply one to many relationship with Excercise
        public ICollection<Excercise> ExcerciseItems { get; set; } = new List<Excercise>();
       
    }
