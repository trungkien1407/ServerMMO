using Gameserver.core.Models;
using Gameserver.core.Dto;

namespace Gameserver.core.Services
{
    public interface ICharacterService
    {
        Task<CharacterDTO?> GetByUserIdAsyncClient(int userId);
        Task<Characters?> GetByUserIdAsync(int userId);
        Task<CharacterDTO?> CreateAsync(Characters character);
        Task<bool> UpdateAsync(Characters character);
        Task<bool> CheckName(string charname);

        Task LoadCharacterDataIntoSession(Session session,int Class);
    }
}
