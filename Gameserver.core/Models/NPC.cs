using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Models
{
    public class NPC
    {
        public int NPCID { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public int MapID    { get; set; }
        public float Y { get; set; }
        public float X { get; set; }
        public int Level { get; set; }
        public int Health { get; set; }
        public int IsHostile { get; set; }
    }
}
