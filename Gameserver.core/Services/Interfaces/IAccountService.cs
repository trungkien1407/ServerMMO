using Gameserver.core.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Services.Interfaces
{
    public interface IAccountService
    {
        Task<string> Register(string username, string password, string email);
        Task<LoginResponse> Login(string username,string password);
       
    }
}
