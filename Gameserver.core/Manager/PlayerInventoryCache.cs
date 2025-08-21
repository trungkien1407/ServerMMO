using Gameserver.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Manager
{
    public class CharacterInventoryCache
    {
       
            public int InventoryId { get; set; }
            public int MaxSlots { get; set; }
            // Key: InventoryItemID, Value: đối tượng InventoryItem lấy từ DB
            public Dictionary<int, InventoryItem> Items { get; private set; } = new Dictionary<int, InventoryItem>();
        
    }

    public class CharacterEquipmentCache
    {
        // Lưu InventoryItemID, không phải ItemID
        public int? WeaponSlot_InventoryItemID { get; set; }
        public int? AoSlot_InventoryItemID { get; set; }
        public int? QuanSlot_InventoryItemID { get; set; }
        public int? GiaySlot_InventoryItemID { get; set; }
        public int? GangTaySlot_InventoryItemID { get; set; }
        public int? BuaSlot_InventoryItemID { get; set; }

    }
}
