using Gameserver.core.Models;
using Gameserver.core.Dto;
using Gameserver.core.Repo.Interfaces;
    using Gameserver.core.Services.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

namespace Gameserver.core.Services
{
    internal class AccountService : IAccountService
    {
        private IAccountRepo _repo;
        public AccountService(IAccountRepo repo)
        {
            _repo = repo;
        }
        public async Task<string> Register(string username, string password, string email)
        {
            var resuilt = await _repo.ExitsNameOrEmail(username, email);
            switch (resuilt)
            {
                case 0: return "Username da ton tai";

                case 1: return "Email da ton tai";
                case 2:
                    {
                        var account = new User()
                        {
                            Username = username,
                            PasswordHash = password,
                            Email = email

                        };
                        var create = await _repo.Create(account);
                        if (create) { return "Success"; }

                        return "False";
                    }
                default: return "Lỗi ở repo";
            }

        }
        public async Task<LoginResponse> Login(string username, string password)
        {
            var account = await _repo.GetByUsernamePassword(username, password);

            if (account == null)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "sai tai khoan hoac mat khau"
                };
            }

            if (account.Stat == 1) // Stat = 1 là bị ban
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "tai khoan bi khoa"
                };
            }

            return new LoginResponse
            {
                Success = true,
                Message = "Đăng nhập thành công",
                UserID = account.UserID,
            };
        }
    }
}
