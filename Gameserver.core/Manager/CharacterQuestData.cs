using Gameserver.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Manager
{
    internal class CharacterQuestData
    {
        public int CharacterID { get; set; }
        public Dictionary<int, Questprogress> ActiveQuests { get; set; } = new();
        public Dictionary<int, List<QuestObjectiveState>> QuestObjectives { get; set; } = new();
        public HashSet<int> CompletedQuests { get; set; } = new();
    }

}
