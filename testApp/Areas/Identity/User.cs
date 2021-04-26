using ARPG.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ARPG.Areas.Identity
{
    public class User : IdentityUser
    {
        public ICollection<Book> Books { get; set; }
    }
}
