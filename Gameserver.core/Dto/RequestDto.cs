using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Dto
{
    public class LoginRequest
    {
        public string? Action { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
  
}
