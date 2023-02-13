using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Ticketing.WebApi.Models
{
    public class VerifiedTicket
    {
        public int Id { get; set; }
        public string TicketNo { get; set; }
        public string PlateNo { get; set; }
        public string EntranceDate { get; set; }
        public string ExitDate { get; set; }
        public string Duration { get; set; }
        public bool IsCompleted { get; set; }  
    }
}