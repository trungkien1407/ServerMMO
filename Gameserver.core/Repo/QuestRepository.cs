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
    public class QuestRepository : IQuestRepository
    {
        private readonly AppDbContext _context;

        public QuestRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Quest>> GetAllQuestsAsync()
        {
            return await _context.Quest
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Questprogress>> GetQuestProgressByCharacterAsync(int characterId)
        {
            return await _context.Questprogress
                .AsNoTracking()
                .Where(qp => qp.CharacterID == characterId)
                .ToListAsync();
        }

        public async Task<List<Questprogressobjective>> GetQuestObjectivesAsync(int questProgressId)
        {
            return await _context.Questprogressobjective
                .AsNoTracking()
                .Where(obj => obj.QuestProgressID == questProgressId)
                .ToListAsync();
        }

        public async Task SaveQuestProgressAsync(Questprogress progress)
        {
            _context.Questprogress.Add(progress);
            await _context.SaveChangesAsync();
        }

        public async Task SaveQuestObjectiveAsync(Questprogressobjective objective)
        {
            _context.Questprogressobjective.Add(objective);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateQuestProgressAsync(Questprogress progress)
        {
            _context.Questprogress.Update(progress);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateQuestObjectiveAsync(Questprogressobjective objective)
        {
            _context.Questprogressobjective.Update(objective);
            await _context.SaveChangesAsync();
        }

        public async Task<List<QuestTemplateObjective>> GetQuestTemplateObjectivesAsync(int questId)
        {
            return await _context.QuestTemplateObjective
                .AsNoTracking()
                .Where(q => q.QuestID == questId)
                .ToListAsync();
        }


    }

}
