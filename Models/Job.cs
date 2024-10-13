namespace JobPortal.Models
{
    public class Job
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string CompanyName { get; set; }
        public string Location { get; set; }
        public DateTime PostedDate { get; set; }

        public int CreatedByUserId { get; set; }

        public int ApplicationCount { get; set; } = 0;  
        public int FavoriteCount { get; set; } = 0;  
        public int ViewCount { get; set; } = 0;  
    }
}
