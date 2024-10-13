namespace JobPortal.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }


        public string Name { get; set; }  
        public string Surname { get; set; }  
        public int? Age { get; set; }  
        public string Residence { get; set; }  
    }
}
