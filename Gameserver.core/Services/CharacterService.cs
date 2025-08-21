using Gameserver.core.Models;
using Gameserver.core.Repo;
using Gameserver.core.Dto;

namespace Gameserver.core.Services
{
    public class CharacterService : ICharacterService
    {
        private readonly ICharacterRepo _characterRepo;

        public CharacterService(ICharacterRepo characterRepo)
        {
            _characterRepo = characterRepo;
        }

        public async Task<CharacterDTO?> GetByUserIdAsyncClient(int userId)
        {
            var result = await _characterRepo.GetCharactersByUserID(userId);
            if (result == null) return null;

            return new CharacterDTO
            {
                CharacterID = result.CharacterID,
                Name = result.Name,
                Gender = result.Gender,

                // Appearance
                HeadID = result.HeadID,
                BodyID = result.BodyID,
                WeaponID = result.WeaponID,
                PantID = result.PantID,

                // Stats
                Class = result.Class,
                Level = result.Level,
                Exp = result.Exp,
                Health = result.Health,
                CurrentHealth = result.CurrentHealth,
                CurrentMana = result.CurrentMana,
                Mana = result.Mana,
                Strength = result.Strength,
                Intelligence = result.Intelligence,
                Dexterity = result.Dexterity,

                // Position
                X = result.X,
                Y = result.Y,
                MapID = result.MapID,

                // Currency
                Gold = result.Gold,
                CreationDate = result.CreationDate,
                LastPlayTime = result.LastPlayTime,
                SkillPoints = result.SkillPoints,
                
            };
        }

        public async Task<Characters?> GetByUserIdAsync(int userid)
        {
            return await _characterRepo.GetCharactersByUserID(userid);
        }

        public async Task<CharacterDTO?> CreateAsync(Characters character)
        {
            // Business logic có thể thêm vào đây nếu cần
           var result = await _characterRepo.Create(character);
            if (result == null) return null;
            return new CharacterDTO
            {
                CharacterID = result.CharacterID,
                Name = result.Name,
                Gender = result.Gender,
                // Appearance
                HeadID = result.HeadID,
                BodyID = result.BodyID,
                WeaponID = result.WeaponID,
                PantID = result.PantID,
                // Stats
                Class = result.Class,
                Level = result.Level,
                Exp = result.Exp,
                Health = result.Health,
                Mana = result.Mana,
                CurrentHealth = result.CurrentHealth,
                CurrentMana = result.CurrentMana,
                Strength = result.Strength,
                Intelligence = result.Intelligence,
                Dexterity = result.Dexterity,

                // Position
                X = result.X,
                Y = result.Y,
                MapID = result.MapID,

                // Currency
                Gold = result.Gold,
                CreationDate = result.CreationDate,
                LastPlayTime = result.LastPlayTime,

            };
                
        }

        public async Task<bool> UpdateAsync(Characters character)
        {
            // Business logic như validate có thể thêm vào đây
            return await _characterRepo.Update(character);
        }
        public async Task<bool> CheckName(string charname)
        {
            return await _characterRepo.CheckName(charname);
        }

        public async Task LoadCharacterDataIntoSession(Session session,int Class)
        {
          await _characterRepo.LoadCharacterDataIntoSession(session,Class);
        }




    }
}
