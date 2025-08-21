using Gameserver.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Repo.Interfaces
{
    public interface ISkillRepo
    {
        Task<List<Characterskill>> GetSkills(int characterid);
        Task<bool> UpgradeSkill(int characterId, int skillId, int level);
        Task<bool> LernSkill(Characterskill characterskill);
    }
}
