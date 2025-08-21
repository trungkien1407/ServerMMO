using Gameserver.core.Models;
using Gameserver.core.Repo.Context;
using Gameserver.core.Repo.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gameserver.core.Repo
{
    public class AccountRepo : IAccountRepo
    {
        private readonly AppDbContext _context;

        public AccountRepo(AppDbContext context)
        {
            _context = context;
        }
        public async Task<bool> Create(User account)
        {
            _context.Accounts.Add(account);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

       public async Task<IEnumerable<User>> GetAll()
        {
          var accounts = await _context.Accounts.ToListAsync();
            return accounts;
        }
        public async Task<bool> Delete(string username)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Username == username);
            if(account == null)
            {
                return false;
            }
            _context.Accounts.Remove(account);
            var result = await _context.SaveChangesAsync();
            return result > 0;

        }
        public async Task<User?> GetById(int id)
        {
            return await _context.Accounts.FirstOrDefaultAsync(a => a.UserID == id);
        }

        public async Task<User?> GetByUsernamePassword(string username,string password)
         {
               return  await _context.Accounts.FirstOrDefaultAsync(a => a.Username == username && a.PasswordHash == password);
               
               
         }

        public async Task<byte> ExitsNameOrEmail(string username, string email)
        {
            var usernameUse = await _context.Accounts.FirstOrDefaultAsync(u => u.Username == username);
            if (usernameUse != null)
            {
                return 0; // username da ton tai
                
            }
            var emaillExits = await _context.Accounts.FirstOrDefaultAsync(u => u.Email == email);
            if (emaillExits != null) 
            {
                return 1; // email da ton tai
            }
            return 2; // ok

        }
       

    }
}
