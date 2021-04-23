using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ARPG.Models.Data
{
    public class ARPGContext : DbContext
    {
        public ARPGContext(DbContextOptions<ARPGContext> options)
            : base(options)
        {
        }
        public DbSet<Action> Action { get; set; }
        public DbSet<Book> Book { get; set; }

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    modelBuilder.Entity<Action>()
        //        .HasOne(a => a.book)
        //        .WithMany(b => b.Actions)
        //        .HasForeignKey(a => a.BookId)
        //        .HasConstraintName("ForeignKey_Actions_Book");
        //}

    }
}