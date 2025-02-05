namespace diplom.Models
{
    public class CategoryType
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int ProjectID { get; set; }
        public Project Project { get; set; }

        public List<Issue> Issues { get; set; }
        public List<UserProject> UserProjects { get; set; }
    }
}
