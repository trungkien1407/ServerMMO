using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Dto
{
    public class InventoryItemDto
    {
        public int InventoryItemID { get; set; }
        public int ItemID { get; set; }
        public int Quantity { get; set; }
        public int SlotNumber { get; set; }
        public int Equiped {  get; set; }
        public int IsLocked { get; set; }
        // Không có thuộc tính Inventory ở đây!
    }

    public class InventoryDto
    {
        public int InventoryId { get; set; }
        public int MaxSlots { get; set; }
        // Dùng Dictionary<int, InventoryItemDto> để giống cấu trúc cache của bạn
        public Dictionary<int, InventoryItemDto> Items { get; set; } = new();
    }
}
