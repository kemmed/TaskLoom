namespace taskloom.Models
{
    public class StatusType
    {
#pragma warning disable CS8618
        public int ID { get; set; }
        public required string Name { get; set; }

        public int ProjectID { get; set; }
        public Project Project { get; set; }
    }
}
