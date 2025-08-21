using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Dto
{
    public class GroundItem
    {
        public Guid UniqueId { get; } = Guid.NewGuid(); // ID duy nhất cho mỗi vật phẩm rơi ra
        public int ItemID { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public int OwnerCharacterID { get; set; } // ID của người chơi sở hữu
        public DateTime ExpiryTime { get; set; } 
    }
}
