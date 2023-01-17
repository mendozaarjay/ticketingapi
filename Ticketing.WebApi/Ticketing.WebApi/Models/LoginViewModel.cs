namespace Ticketing.WebApi.Models
{
    public class LoginViewModel
    {
        public string Key { get; set; }
        public bool IsValid { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
    }
}