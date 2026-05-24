using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitPanel.Data.Models;

    public class Client
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? InbodyLink { get; set; }
        public string? FromPicLink { get; set; }
        public string? ToPicLink { get; set; }
        
        public double Weight { get; set; }
        public double Bmr { get; set; }
       public DateTime CreatedAt { get; set; }
       public DateTime StartDate { get; set; }
       public DateTime EndDate { get; set; }
       public string? Goal { get; set; }
      

        public int SubscriptionDurationPerMonth { get; set; }
         // FK → Coach (PanelUser)
    public string CoachId { get; set; } = string.Empty;   // string because Identity uses GUID
    public PanelUser Coach { get; set; } = null!;


        //One to Many relationship with Diet

         public ICollection<Diet> Diets { get; set; } = new List<Diet>();
         public ICollection<WorkOut> WorkOuts { get; set; } = new List<WorkOut>();
    }
