using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gameserver.core.Models
{
    public class Monsters
    {
        [Key]
       public int MonsterId { get; set; }
        public string MonsterName { get; set; }
        public string MonsterImg { get; set; }
        public int HP {  get; set; }
        public int Strength { get; set; }
        public string DropItems { get; set; }
        public int Level { get; set; }
        public ICollection<Monsterspawn>? MonsterSpawns { get; set; }
    }

    public class Monsterspawn
    {
        [Key]
        public int SpawnID { get; set; }
        public int MapID { get; set; }
        public int MonsterID { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public int RespawnTime { get; set; }

        public Monsters? Monster { get; set; }
    }
}
