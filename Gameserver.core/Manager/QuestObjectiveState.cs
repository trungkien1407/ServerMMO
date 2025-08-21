using Gameserver.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Manager
{
    public class QuestObjectiveState
    {
        public Questprogressobjective Original { get; set; } // dữ liệu gốc từ DB
        public int TempAmount { get; set; } // số lượng đã làm được trong RAM
        public bool IsComplete => TempAmount >= Original.RequiredAmount;
        public bool IsDirty => TempAmount != Original.CurrentAmount; // có thay đổi so với DB
    }

}
