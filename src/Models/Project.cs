namespace diplom.Models
{
    public enum ProjectStatus {InProcess, Completed, Frozen}
    
    public class Project
    {

        public int ID { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? DeadlineDate { get; set; }
        public DateTime CreateDate { get; set; }
        public ProjectStatus Status { get; set; }
        public bool IsDelete { get; set; }


        public List<Issue> Issues { get; set; }
        public List<string> PriorityTypes { get; set; }
        public List<string> StatusTypes { get; set; }
        public List<string>? CategoryTypes { get; set; }
        public List<UserProject> UserProjects { get; set; }
        public List<Log> Logs { get; set; }

        public string ConvertStatus(ProjectStatus projectStatus)
        {
            switch (projectStatus)
            {
                case ProjectStatus.Completed:
                    return "Завершен";
                case ProjectStatus.InProcess:
                    return "В процессе";
                case ProjectStatus.Frozen:
                    return "Отложен";
                default:
                    return "";

            }
        }
    }
    
}
