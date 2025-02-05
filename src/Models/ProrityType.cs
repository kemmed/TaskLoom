namespace diplom.Models
{
    public class PriorityType
    {
        public int ID { get; set; }
        public string Name { get; set; }

        public int ProjectID { get; set; }
        public Project Project { get; set; }

        public List<Issue> Issues { get; set; }
    }
}
