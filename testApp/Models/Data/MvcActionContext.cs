using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace testApp.Models.Data
{
    public class MvcActionContext : DbContext
    {
        public MvcActionContext(DbContextOptions<MvcActionContext> options)
            : base(options)
        {
        }

        public DbSet<Action> Action { get; set; }
    }
}