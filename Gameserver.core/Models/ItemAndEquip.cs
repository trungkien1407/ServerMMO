using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Models
{
    public class Item
    {
        [Key]
        public int ItemID { get; set; }
        public string Name { get; set; } // Fixed: should be string, not int
        public string Type { get; set; }
        public int RequiredLevel { get; set; } // Fixed typo: RequierdLevel -> RequiredLevel
        public string Description { get; set; } // Fixed typo: Desciption -> Description
        public int BuyPrice { get; set; }
        public int MaxStackSize { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class Inventory
    {
        [Key]
        public int InventoryID { get; set; }
        public int CharacterID { get; set; }
        public int MaxSlots { get; set; }

        // Navigation properties
        public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
    }

    public class InventoryItem
    {
        [Key]
        public int InventoryItemID { get; set; }
        public int InventoryID { get; set; }
        public int ItemID { get; set; }
        public int Quantity { get; set; }
        public int SlotNumber { get; set; }
        public int Equipped { get; set; } // Changed to bool for clarity
        public int IsLocked { get; set; } // Changed to bool for clarity
        public DateTime AcquiredDate { get; set; }

        // Navigation properties
        public virtual Inventory Inventory { get; set; }
        public virtual Item Item { get; set; }
    }

    public class CharacterEquipment // Fixed class name
    {
        [Key]
        public int EquipmentID { get; set; }
        public int CharacterID { get; set; }
        public int? WeaponSlot { get; set; }
        public int? SlotAo { get; set; } // Armor slot
        public int? SlotQuan { get; set; } // Pants slot
        public int? SlotGiay { get; set; } // Shoes slot
        public int? SlotGangTay { get; set; } // Gloves slot
        public int? SlotBua { get; set; } // Axe/Tool slot
        public DateTime LastUpdate { get; set; }
    }



public class EquipmentData
    {
        [Key, ForeignKey(nameof(Item))] // Gộp lại rõ ràng
        public int ItemID { get; set; }

        public int SlotType { get; set; }
        public int RequiredLevel { get; set; }
        public int ClassRestriction { get; set; } = -1;
        public int GenderRestriction { get; set; }
        public int AttackBonus { get; set; }
        public int HPBonus { get; set; }

        public Item Item { get; set; }
    }


    // Enums for better type safety
    public enum EquipmentSlotType
    {
        Weapon = 1,
        Armor = 2,
        Pants = 3,
        Shoes = 4,
        Gloves = 5,
        Tool = 6
    }
    public enum ItemType
    {
        Equipment,
        HP,
        Material,
    }

}
