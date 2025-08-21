using Gameserver.core.Dto;
using Gameserver.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Repo.Interfaces
{
    public interface IInventoryRepo
    {
        // Thêm một item vào túi đồ của nhân vật
        Task<InventoryResult> AddItemToInventory(Session session, int itemId, int quantity);

        // Xóa một item (hoặc giảm số lượng) khỏi túi đồ
        Task<InventoryResult> RemoveItemFromInventory(Session session, int inventoryItemId, int quantity);

        // Di chuyển item giữa các ô trong túi
      //  Task<InventoryResult> MoveItemInInventory(Session session, int inventoryItemId, int newSlot);
    }
    public class InventoryResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        // Chứa thông tin về các item đã bị thay đổi để gửi về client
        public List<InventoryItem> UpdatedItems { get; set; } = new List<InventoryItem>();
        public List<int> RemovedItemIds { get; set; } = new List<int>();
    }
}
