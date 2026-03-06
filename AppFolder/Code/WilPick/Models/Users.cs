using Microsoft.AspNetCore.Identity;

namespace WilPick.Models
{
    public class Users : IdentityUser
    {
        public string FullName { get; set; }
    }
}
