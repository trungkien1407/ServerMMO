using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gameserver.core.Dto;
using Gameserver.core.Models;
using Gameserver.core.Repo.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Gameserver.core.Manager
{
    
    public class SkillManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<int, Dictionary<int, SkillRuntime>> _skillCache = new();

        public SkillManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task LoadSkillsIfNeededAsync(int characterId)
        {
            if (_skillCache.ContainsKey(characterId)) return;

            using var scope = _serviceProvider.CreateScope();
            var skillRepo = scope.ServiceProvider.GetRequiredService<ISkillRepo>();

            var characterSkills = await skillRepo.GetSkills(characterId); 

            var runtimeDict = new Dictionary<int, SkillRuntime>();
            foreach (var cs in characterSkills)
            {
                if (cs.Skill != null)
                    runtimeDict[cs.SkillID] = new SkillRuntime(cs.Skill, cs);
            }

            _skillCache[characterId] = runtimeDict;
        }
        public int CaculateDameage(SkillRuntime skillRuntime)
        {
           return skillRuntime.CalculateDamage();

        }

        public SkillRuntime? GetSkill(int characterId, int skillId)
        {
            if (_skillCache.TryGetValue(characterId, out var skills) &&
                skills.TryGetValue(skillId, out var skill))
            {
                return skill;
            }
            return null;
        }
        public Dictionary<int, SkillRuntime> GetSkills(int characterID)
        {
            if (_skillCache.TryGetValue(characterID, out var skills))
            {
                return skills;
            }

            return new Dictionary<int, SkillRuntime>(); // Trả về rỗng thay vì null để tránh lỗi
        }

        public List<SkillDto> GetSkillDTOs(int characterID)
        {
            var skills = GetSkills(characterID);
            var result = new List<SkillDto>();

            foreach (var skill in skills.Values)
            {
                result.Add(new SkillDto
                {
                    SkillID = skill.SkillID,
                    Name = skill.Name,
                    Level = skill.Level,
                    Cooldown = skill.GetCooldown(),
                });
            }

            return result;
        }

        public bool TryUseSkill(int characterId, int skillId, out SkillRuntime? skill)
        {
            skill = GetSkill(characterId, skillId);
            if (skill == null) return false;
            return skill.Use();
        }

        public void UnloadSkills(int characterId)
        {
            _skillCache.Remove(characterId);
        }
    }

}
