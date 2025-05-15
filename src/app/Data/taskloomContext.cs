using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using taskloom.Models;

namespace taskloom.Data
{
    public class taskloomContext : DbContext
    {
        public taskloomContext (DbContextOptions<taskloomContext> options)
            : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Issue>()
                .HasOne(i => i.Creator)
                .WithMany(u => u.Issues)
                .HasForeignKey(i => i.CreatorID)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Issue>()
                .HasOne(i => i.Performer)
                .WithMany()
                .HasForeignKey(i => i.PerformerID)
                .OnDelete(DeleteBehavior.SetNull);

            //modelBuilder.Entity<Project>()
            //   .HasMany(p => p.CategoryTypes)
            //   .WithOne(ct => ct.Project)
            //   .HasForeignKey(ct => ct.ProjectID);
        }


        public DbSet<taskloom.Models.User> User { get; set; } = default!;
        public DbSet<taskloom.Models.Project> Project { get; set; } = default!;
        public DbSet<taskloom.Models.UserProject> UserProject { get; set; } = default!;
        public DbSet<taskloom.Models.CategoryType> CategoryType { get; set; } = default!;
        public DbSet<taskloom.Models.Issue> Issue { get; set; } = default!;
        public DbSet<taskloom.Models.Log> Log { get; set; } = default!;
        public DbSet<taskloom.Models.PriorityType> PriorityType { get; set; } = default!;
        public DbSet<taskloom.Models.StatusType> StatusType { get; set; } = default!;

    }
}
