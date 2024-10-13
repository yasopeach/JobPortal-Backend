namespace JobPortal.Models
{
    public class Application
    {
        public int Id { get; set; }
        public int JobId { get; set; }  
        public string ApplicantUsername { get; set; } 
        public string ApplicantEmail { get; set; } 
        public string Status { get; set; } = "Pending";  
        public string CvFileName { get; set; }  
        public string CvFilePath { get; set; }  
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  


        public Job Job { get; set; }  
    }
}
