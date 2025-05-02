namespace diplom.Models
{
    public enum UserRoles { Admin, Manager, Employee}
    public class UserProject
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public int ProjectID { get; set; }
        public UserRoles UserRole { get; set; }
        public string? InviteToken { get; set; }
        public DateTime? InviteTokenDate { get; set; }
        public bool IsCreator { get; set; }
        public bool IsActive { get; set; }

        public User User { get; set; }
        public Project Project { get; set; }

        public List<CategoryType> CategoryTypes { get; set; }

        public string ConvertRoles(UserRoles userRoles)
        {
            switch (userRoles)
            {
                case UserRoles.Admin:
                    return "Администратор";
                case UserRoles.Manager:
                    return "Менеджер";
                case UserRoles.Employee:
                    return "Сотрудник";
                default:
                    return "";

            }
        }
    }
}
