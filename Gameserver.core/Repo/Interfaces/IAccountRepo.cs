using Gameserver.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Repo.Interfaces
{
    internal interface IAccountRepo
    {
        Task<IEnumerable<User>> GetAll();       // Lấy tất cả account
        Task<User?> GetById(int id);              // Lấy account theo id
        Task<bool> Delete(string username);         // Xóa account
        Task<bool> Create(User account);
        Task<User?> GetByUsernamePassword(string username, string password);
        Task<byte> ExitsNameOrEmail(string username, string email);
      
    }

}
