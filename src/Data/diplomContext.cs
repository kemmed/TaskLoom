using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using diplom.Models;

namespace diplom.Data
{
    public class diplomContext : DbContext
    {
        public diplomContext (DbContextOptions<diplomContext> options)
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


        public DbSet<diplom.Models.User> User { get; set; } = default!;
        public DbSet<diplom.Models.Project> Project { get; set; } = default!;
        public DbSet<diplom.Models.UserProject> UserProject { get; set; } = default!;
        public DbSet<diplom.Models.CategoryType> CategoryType { get; set; } = default!;
        public DbSet<diplom.Models.Issue> Issue { get; set; } = default!;
        public DbSet<diplom.Models.Log> Log { get; set; } = default!;
        public DbSet<diplom.Models.PriorityType> PriorityType { get; set; } = default!;
        public DbSet<diplom.Models.StatusType> StatusType { get; set; } = default!;

    }
}
