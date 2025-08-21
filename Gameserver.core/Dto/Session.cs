using Gameserver.core.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Dto
{
    public class Session
    {
        public int UserID { get; set; }
        public Guid ClientId { get; set; } // Watson WebSocket's clientId
        public CharacterDTO? Character { get; set; }
        public int CharacterID { get; set; }
        public int MapID { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public CharacterInventoryCache InventoryCache { get; private set; } = new CharacterInventoryCache();
        public CharacterEquipmentCache EquipmentCache { get; private set; } = new CharacterEquipmentCache();

    }

}
