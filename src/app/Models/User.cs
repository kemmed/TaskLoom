﻿namespace taskloom.Models
{
    public class User
    {
#pragma warning disable CS8618
        public int ID { get; set; }
        public string? FName { get; set; }
        public string? LName { get; set; }
        public required string Email { get; set; }
        public required string HashPass { get; set; }
        public bool IsActive { get; set; }
        public string? RegToken {  get; set; }
        public DateTime? RegTokenDate {  get; set; }
        public string? PassRecoveryToken {  get; set; }
        public DateTime? PassRecoveryTokenDate {  get; set; }
        public List <UserProject> UserProjects { get; set; }
        public List <Project>? Projects { get; set; }
        public List <Issue>? Issues { get; set; }
    }
}
