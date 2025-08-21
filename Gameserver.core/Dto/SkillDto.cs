using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Dto
{
    public class SkillDto
    {
        public int SkillID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Level { get; set; }
        public float Cooldown { get; set; }

    }
}
