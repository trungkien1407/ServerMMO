using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Gameserver.core.Models
{
    public class Map
    {
        public int MapID { get; set; }
        public string? Name { get; set; }
    }

}
