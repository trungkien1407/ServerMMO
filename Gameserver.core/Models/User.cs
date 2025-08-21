using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Models
{
    [Table("user")]
    public class User
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Email { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public DateTime LastLoginDate { get; set; } = DateTime.UtcNow;
        public int Stat { get; set; } = 0;
        public int Premium { get; set; } = 0;
        public int isAdmin { get; set; } = 0;
        public int isOnline { get; set; } = 0;

        //public Characters? Characters { get; set; }

    }
}
