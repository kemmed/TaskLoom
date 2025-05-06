namespace diplom.Models
{
    public class PriorityType
    {
        public int ID { get; set; }
        public required string Name { get; set; }

        public int ProjectID { get; set; }
        public Project Project { get; set; }
    }
}
