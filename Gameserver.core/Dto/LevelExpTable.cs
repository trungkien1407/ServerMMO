using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Dto
{
    public static class LevelExpTable
    {
        private static readonly Dictionary<int, int> requiredExpPerLevel = new Dictionary<int, int>
        {
            { 2, 50 },
            { 3, 100 },
            { 4, 200 },
            { 5, 300 },
            { 6, 800 },
            { 7, 1500 },
            { 8, 2500},
            { 9, 3500 },
            { 10, 5000 },
            { 11, 6500 },
            { 12, 8000 },
            { 13, 10000 },
            { 14, 14000 },
            { 16, 18000 },
            { 17, 28000 },
            { 18, 38000 },
            { 19, 48000 },
            // … tiếp tục tùy ý
        };

        public static int GetRequiredExp(int level)
        {
            return requiredExpPerLevel.TryGetValue(level, out var exp) ? exp : int.MaxValue;
        }

        public static int MaxLevel => requiredExpPerLevel.Keys.Max();
    }
}
