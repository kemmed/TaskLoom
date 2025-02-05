namespace diplom.Models
{
    public enum UserRoles { Admin, Manager, Employee}
    public class UserProject
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public int ProjectID { get; set; }
        public UserRoles UserRole { get; set; }
        public User User { get; set; }
        public Project Project { get; set; }

        public List<CategoryType> CategoryTypes { get; set; }
    }
}
