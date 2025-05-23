﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using taskloom.Data;

#nullable disable

namespace taskloom.Migrations
{
    [DbContext(typeof(taskloomContext))]
    partial class taskloomContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.5");

            modelBuilder.Entity("CategoryTypeUserProject", b =>
                {
                    b.Property<int>("CategoryTypesID")
                        .HasColumnType("INTEGER");

                    b.Property<int>("UserProjectsID")
                        .HasColumnType("INTEGER");

                    b.HasKey("CategoryTypesID", "UserProjectsID");

                    b.HasIndex("UserProjectsID");

                    b.ToTable("CategoryTypeUserProject");
                });

            modelBuilder.Entity("taskloom.Models.CategoryType", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("ProjectID")
                        .HasColumnType("INTEGER");

                    b.HasKey("ID");

                    b.HasIndex("ProjectID");

                    b.ToTable("CategoryType");
                });

            modelBuilder.Entity("taskloom.Models.Issue", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("CategoryTypeID")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreateDate")
                        .HasColumnType("TEXT");

                    b.Property<int>("CreatorID")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("DeadlineDate")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DeleteDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("EndDate")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsDelete")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int?>("PerformerID")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PriorityTypeID")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ProjectID")
                        .HasColumnType("INTEGER");

                    b.Property<int>("StatusTypeID")
                        .HasColumnType("INTEGER");

                    b.HasKey("ID");

                    b.HasIndex("CategoryTypeID");

                    b.HasIndex("CreatorID");

                    b.HasIndex("PerformerID");

                    b.HasIndex("PriorityTypeID");

                    b.HasIndex("ProjectID");

                    b.HasIndex("StatusTypeID");

                    b.ToTable("Issue");
                });

            modelBuilder.Entity("taskloom.Models.Log", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("DateTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("Event")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("ProjectID")
                        .HasColumnType("INTEGER");

                    b.HasKey("ID");

                    b.HasIndex("ProjectID");

                    b.ToTable("Log");
                });

            modelBuilder.Entity("taskloom.Models.PriorityType", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("ProjectID")
                        .HasColumnType("INTEGER");

                    b.HasKey("ID");

                    b.HasIndex("ProjectID");

                    b.ToTable("PriorityType");
                });

            modelBuilder.Entity("taskloom.Models.Project", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreateDate")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DeadlineDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("EndDate")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsDelete")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("UserID")
                        .HasColumnType("INTEGER");

                    b.HasKey("ID");

                    b.HasIndex("UserID");

                    b.ToTable("Project");
                });

            modelBuilder.Entity("taskloom.Models.StatusType", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("ProjectID")
                        .HasColumnType("INTEGER");

                    b.HasKey("ID");

                    b.HasIndex("ProjectID");

                    b.ToTable("StatusType");
                });

            modelBuilder.Entity("taskloom.Models.User", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("FName")
                        .HasColumnType("TEXT");

                    b.Property<string>("HashPass")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsActive")
                        .HasColumnType("INTEGER");

                    b.Property<string>("LName")
                        .HasColumnType("TEXT");

                    b.Property<string>("PassRecoveryToken")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("PassRecoveryTokenDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("RegToken")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("RegTokenDate")
                        .HasColumnType("TEXT");

                    b.HasKey("ID");

                    b.ToTable("User");
                });

            modelBuilder.Entity("taskloom.Models.UserProject", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("InviteToken")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("InviteTokenDate")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsActive")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsCreator")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ProjectID")
                        .HasColumnType("INTEGER");

                    b.Property<int>("UserID")
                        .HasColumnType("INTEGER");

                    b.Property<int>("UserRole")
                        .HasColumnType("INTEGER");

                    b.HasKey("ID");

                    b.HasIndex("ProjectID");

                    b.HasIndex("UserID");

                    b.ToTable("UserProject");
                });

            modelBuilder.Entity("CategoryTypeUserProject", b =>
                {
                    b.HasOne("taskloom.Models.CategoryType", null)
                        .WithMany()
                        .HasForeignKey("CategoryTypesID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("taskloom.Models.UserProject", null)
                        .WithMany()
                        .HasForeignKey("UserProjectsID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("taskloom.Models.CategoryType", b =>
                {
                    b.HasOne("taskloom.Models.Project", "Project")
                        .WithMany("CategoryTypes")
                        .HasForeignKey("ProjectID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");
                });

            modelBuilder.Entity("taskloom.Models.Issue", b =>
                {
                    b.HasOne("taskloom.Models.CategoryType", "CategoryType")
                        .WithMany()
                        .HasForeignKey("CategoryTypeID");

                    b.HasOne("taskloom.Models.User", "Creator")
                        .WithMany("Issues")
                        .HasForeignKey("CreatorID")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("taskloom.Models.User", "Performer")
                        .WithMany()
                        .HasForeignKey("PerformerID")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("taskloom.Models.PriorityType", "PriorityType")
                        .WithMany()
                        .HasForeignKey("PriorityTypeID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("taskloom.Models.Project", "Project")
                        .WithMany("Issues")
                        .HasForeignKey("ProjectID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("taskloom.Models.StatusType", "StatusType")
                        .WithMany()
                        .HasForeignKey("StatusTypeID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("CategoryType");

                    b.Navigation("Creator");

                    b.Navigation("Performer");

                    b.Navigation("PriorityType");

                    b.Navigation("Project");

                    b.Navigation("StatusType");
                });

            modelBuilder.Entity("taskloom.Models.Log", b =>
                {
                    b.HasOne("taskloom.Models.Project", "Project")
                        .WithMany("Logs")
                        .HasForeignKey("ProjectID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");
                });

            modelBuilder.Entity("taskloom.Models.PriorityType", b =>
                {
                    b.HasOne("taskloom.Models.Project", "Project")
                        .WithMany("PriorityTypes")
                        .HasForeignKey("ProjectID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");
                });

            modelBuilder.Entity("taskloom.Models.Project", b =>
                {
                    b.HasOne("taskloom.Models.User", null)
                        .WithMany("Projects")
                        .HasForeignKey("UserID");
                });

            modelBuilder.Entity("taskloom.Models.StatusType", b =>
                {
                    b.HasOne("taskloom.Models.Project", "Project")
                        .WithMany("StatusTypes")
                        .HasForeignKey("ProjectID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");
                });

            modelBuilder.Entity("taskloom.Models.UserProject", b =>
                {
                    b.HasOne("taskloom.Models.Project", "Project")
                        .WithMany("UserProjects")
                        .HasForeignKey("ProjectID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("taskloom.Models.User", "User")
                        .WithMany("UserProjects")
                        .HasForeignKey("UserID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");

                    b.Navigation("User");
                });

            modelBuilder.Entity("taskloom.Models.Project", b =>
                {
                    b.Navigation("CategoryTypes");

                    b.Navigation("Issues");

                    b.Navigation("Logs");

                    b.Navigation("PriorityTypes");

                    b.Navigation("StatusTypes");

                    b.Navigation("UserProjects");
                });

            modelBuilder.Entity("taskloom.Models.User", b =>
                {
                    b.Navigation("Issues");

                    b.Navigation("Projects");

                    b.Navigation("UserProjects");
                });
#pragma warning restore 612, 618
        }
    }
}
