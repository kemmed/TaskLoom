namespace diplom.Models
{
    public class Issue
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? DeadlineDate { get; set; }

        public int CreatorID { get; set; }

        public int? PerformerID { get; set; }
        public int ProjectID { get; set; }
        public String Priority { get; set; } 

        public bool IsDelete { get; set; }
        public DateTime? DeleteDate { get; set; }
        public String? Category { get; set; }
        public String Status { get; set; }

        public Project Project { get; set; }
        public User Creator { get; set; }
        public User? Performer { get; set; }
    }
}
