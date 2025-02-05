namespace diplom.Models
{
    public class Log
    {
        public int ID { get; set; }
        public DateTime DateTime { get; set; }
        public string Event {  get; set; }
        public int ProjectID { get; set; }

        public Project Project { get; set; }
    }
}
