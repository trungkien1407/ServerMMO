using Gameserver.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Repo.Interfaces
{
    public interface IQuestRepository
    {
        Task<List<Quest>> GetAllQuestsAsync();
        Task<List<Questprogress>> GetQuestProgressByCharacterAsync(int characterId);
        Task<List<Questprogressobjective>> GetQuestObjectivesAsync(int questProgressId);
        Task SaveQuestProgressAsync(Questprogress progress);
        Task SaveQuestObjectiveAsync(Questprogressobjective objective);
        Task UpdateQuestProgressAsync(Questprogress progress);
        Task UpdateQuestObjectiveAsync(Questprogressobjective objective);
        Task<List<QuestTemplateObjective>> GetQuestTemplateObjectivesAsync(int questId);

       
       
    }
}
