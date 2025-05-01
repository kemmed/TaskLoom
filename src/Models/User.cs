namespace diplom.Models
{
    public class User
    {
        public int ID { get; set; }
        public string? FName { get; set; }
        public string? LName { get; set; }
        public string Email { get; set; }
        public string HashPass { get; set; }
        public bool IsActive { get; set; }
        public string? RegToken {  get; set; }
        public DateTime? RegTokenDate {  get; set; }
        public string? PassRecoveryToken {  get; set; }
        public DateTime? PassRecoveryTokenDate {  get; set; }

        public List <UserProject> UserProjects { get; set; }
    }
}
