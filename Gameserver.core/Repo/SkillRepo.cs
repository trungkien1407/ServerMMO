using Gameserver.core.Models;
using Gameserver.core.Repo.Context;
using Gameserver.core.Repo.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Repo
{
    public class SkillRepo :ISkillRepo
    {
        private readonly AppDbContext _context;

        public SkillRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Characterskill>> GetSkills(int characterid)
        {
            return await _context.CharacterSkills.Include(skill => skill.Skill).Where(skill => skill.CharacterID == characterid).ToListAsync();
        }
        public async Task<bool> UpgradeSkill(int characterId, int skillId, int level)
        {
            var charSkill = await _context.CharacterSkills
                .FirstOrDefaultAsync(cs => cs.CharacterID == characterId && cs.SkillID == skillId);

            if (charSkill == null) return false;
            charSkill.Level = level;
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> LernSkill(Characterskill characterskill)
        {
            var skill = await _context.CharacterSkills.AddAsync(characterskill);
          var result =  await _context.SaveChangesAsync();
            if(result>0) return true;
            else return false;
        }

    }
}
