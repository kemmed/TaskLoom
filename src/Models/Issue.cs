namespace diplom.Models
{
    public class Issue
    {
#pragma warning disable CS8618
        public int ID { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? DeadlineDate { get; set; }

        public int CreatorID { get; set; }

        public int? PerformerID { get; set; }
        public int ProjectID { get; set; }
        public int PriorityTypeID { get; set; } 

        public bool IsDelete { get; set; }
        public DateTime? DeleteDate { get; set; }
        public int? CategoryTypeID { get; set; }
        public int StatusTypeID { get; set; }


        public StatusType StatusType { get; set; }
        public CategoryType? CategoryType { get; set; }
        public PriorityType PriorityType { get; set; }
        public Project Project { get; set; }
        public User Creator { get; set; }
        public User? Performer { get; set; }
    }
}
