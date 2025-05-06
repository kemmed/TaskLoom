namespace diplom.Models
{
    public class StatusType
    {
        public int ID { get; set; }
        public required string Name { get; set; }

        public int ProjectID { get; set; }
        public Project Project { get; set; }
    }
}
