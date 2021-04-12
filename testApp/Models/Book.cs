using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ARPG.Models
{
    public class Book
    {
        public Book(){}

        public int Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public bool IsValid { get; set; }
    }
}
