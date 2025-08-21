using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Models
{
    public class Skill
    {
        [Key]
        public int SkillID { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int RequiredLevel { get; set; }
        public int ManaCost { get; set; }
        public int Cooldown {  get; set; }
        public int Damage { get; set; }
        public int size { get; set; }
        public int Class {  get; set; }
        public int MaxLevel { get; set; } = 10;
        public ICollection<Characterskill>? CharacterSkill { get; set; }
    }

    public class Characterskill
    {
        [Key]
        public int CharacterSkillID { get; set; }
        public int CharacterID { get; set; }
        public int SkillID { get; set; }
        public int Level { get; set; }
        public Skill? Skill { get; set; }
        public Characters? Character { get; set; } 
    }
}
