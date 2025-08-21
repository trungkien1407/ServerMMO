using Gameserver.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Dto
{
    public class CharacterDTO
    {
        public int CharacterID { get; set; }
        public string? Name { get; set; }
        public int Gender { get; set; }  
        public int HeadID { get; set; } 
        public int BodyID { get; set; } 
        public int WeaponID { get; set; } 
        public int PantID { get; set; } 
        public int Class { get; set; }
        public int Level { get; set; } 
        public int Exp { get; set; } 
        public int Health { get; set; }
        public int CurrentHealth { get; set; }
        public int CurrentMana { get; set; } 
        public int Mana { get; set; }
        public int Strength { get; set; } 
        public int Intelligence { get; set; } 
        public int Dexterity { get; set; } 
        public float X { get; set; }
        public float Y { get; set; }
        public int MapID { get; set; } 
        public int Gold { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastPlayTime { get; set; }
        public int SkillPoints { get; set; }
        public CharacterDTO()
        {

        }
        public void Heal(int amount)
        {
            CurrentHealth = Math.Min(CurrentHealth + amount, Health);
        }

        public void RestoreMana(int amount)
        {
            CurrentMana = Math.Min(CurrentMana + amount, Mana);
        }
        public void TakeDamage(int amount)
        {
            CurrentHealth = Math.Max(CurrentHealth - amount, 0);
        }
        public bool IsDead()
        {
            return CurrentHealth <= 0;
        }
        public void UpdatePositon(float x, float y)
        {   
            X = x; Y = y;
        }
        public bool AddExp(int amount)
        {
            Exp += amount;
            bool leveledUp = false;

            while (Level < LevelExpTable.MaxLevel &&
                   Exp >= LevelExpTable.GetRequiredExp(Level + 1))
            {
                Level++;
                leveledUp = true;

                // Có thể cộng chỉ số tăng theo cấp ở đây
                Health += 50;
                Mana += 50;
                Strength += 5;
                Intelligence += 2;
                Dexterity += 1;
                SkillPoints += 1;
                // Hồi máu + mana đầy sau khi lên cấp
                CurrentHealth = Health;
                CurrentMana = Mana;
            }

            return leveledUp;
        }

        public float ExpPercentToNextLevel
        {
            get
            {
                // Nếu đang ở cấp tối đa
                if (Level >= LevelExpTable.MaxLevel)
                    return 99.99f;

                int expCurrent = Exp - LevelExpTable.GetRequiredExp(Level);
                int expRequired = LevelExpTable.GetRequiredExp(Level + 1) - LevelExpTable.GetRequiredExp(Level);

                if (expRequired <= 0) return 0f;

                return (expCurrent / (float)expRequired) * 100f;
            }
        }





    }

}

