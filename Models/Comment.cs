namespace JobPortal.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string Username { get; set; }  
        public int JobId { get; set; }  
        public string Content { get; set; }  
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  
    }
}
