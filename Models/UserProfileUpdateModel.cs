namespace JobPortal.Models
{
    public class UserProfileUpdateModel
    {
        public string Email { get; set; }
        public string Role { get; set; }
        public string Name { get; set; }  
        public string Surname { get; set; }  
        public int? Age { get; set; }  
        public string Residence { get; set; }  
    }
}
