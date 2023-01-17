namespace Ticketing.WebApi.Models
{
    public class LoginModel
    {
        public bool IsValid { get; set; }
        public string UserName { get; set; }    
        public int UserId { get; set; }
        public string Name { get; set; }    

    }
}