namespace diplom.Models
{
    public class Log
    {
#pragma warning disable CS8618
        public int ID { get; set; }
        public DateTime DateTime { get; set; }
        public required string Event {  get; set; }
        public int ProjectID { get; set; }

        public Project Project { get; set; }
    }
}
