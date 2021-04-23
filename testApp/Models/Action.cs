using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ARPG.Models
{
    public class Action
    {
        public Action() { }


        public int Id { get; set; }

        public int BookId { get; set; }
        public Book Book { get; set; }
        public int ActionNumber { get; set; }
        public string ActionMessage { get; set; }
        public string SuccessorMessage1 { get; set; }
        public int SuccessorCode1 { get; set; }
        public string SuccessorMessage2 { get; set; }
        public int SuccessorCode2 { get; set; }
        public int HPGains { get; set; }
        public bool? IsWon {get; set;}
    }
}