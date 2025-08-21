using Gameserver.core.Models;
using Gameserver.core.Network;
using Gameserver.core.Repo.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace Gameserver.core.Manager
{
    public class SkillRuntime
    {
        public int SkillID { get; }
        public string? Name { get; }
        public string? Description { get; }
        public int RequiredLevel { get; }
        public int ManaCost { get; }
        public float BaseCooldown { get; }
        public int BaseDamage { get; }
        public int Class { get; }
        public int CharacterID { get; }
        public int Level { get; private set; }

       
        private DateTime lastUsedTime = DateTime.MinValue;
        public SkillRuntime(Skill skill, Characterskill characterskill)
        {
            this.SkillID = skill.SkillID;
            this.Name = skill.Name;
            this.Description = skill.Description;
            this.RequiredLevel = skill.RequiredLevel;
            this.ManaCost = skill.ManaCost;
            this.BaseCooldown = skill.Cooldown;
            this.BaseDamage = skill.Damage;
            this.Class = skill.Class;
            this.CharacterID = characterskill.CharacterID;
            this.Level = characterskill.Level;
        }

        public float GetCooldown()
        {
            // Giảm cooldown theo level skill
            return Math.Max(0f, BaseCooldown - Level * 0.05f);
        }

        public int CalculateDamage()
        {
            // Công thức tính damage, ví dụ damage cơ bản * level
            return BaseDamage + Level * 10;
        }

        public bool CanUse()
        {
            return (DateTime.UtcNow - lastUsedTime).TotalSeconds >= GetCooldown();
        }

        public bool Use()
        {
            if (!CanUse()) return false;

            lastUsedTime = DateTime.UtcNow;
            return true;
        }

        public int UpLevel()
        {
            if (Level < 10) return Level += 1;
            return Level;
        }
    }


}
