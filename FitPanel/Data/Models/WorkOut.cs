using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitPanel.Data.Models;

    public class WorkOut
    {
        public int Id { get; set; }
        public string SplitName { get; set; }
        public int NumberOfWorkOutDays { get; set; }
       public DateTime CreatedAt { get; set; }

        // Foreign Key to Client
        public int ClientId { get; set; }   // FK
        public Client Client { get; set; }  // Navigation

        //Apply one to many relationship with WorkOutItem
        public ICollection<WorkOutDay> WorkOutDays { get; set; } = new List<WorkOutDay>();
    }
