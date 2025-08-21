    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace Gameserver.core.Manager
    {
        public class CachedItemData
        {
            // Thông tin từ bảng Item
            public int ItemID { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public int RequiredLevel { get; set; }
            public string Description { get; set; }
            public int BuyPrice { get; set; }
            public int MaxStackSize { get; set; }
            public bool IsEquippable { get; set; }

            // Thông tin từ bảng EquipmentData (có thể null nếu không phải trang bị)
            public int? SlotType { get; set; }
            public int ClassRestriction { get; set; }
            public int? GenderRestriction { get; set; }
            public int? AttackBonus { get; set; }
            public int? HPBonus { get; set; }
 
        }
    }
