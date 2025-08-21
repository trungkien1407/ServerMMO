using Gameserver.core.Dto;
using Gameserver.core.Manager;
using Gameserver.core.Models;
using Gameserver.core.Repo.Context;
using Gameserver.core.Repo.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameserver.core.Repo
{
    internal class InventoryRepo : IInventoryRepo
    {
        private readonly AppDbContext _dbContext; // Cần DbContext để tạo item mới

        private readonly ItemDataManager _itemManager;

        public InventoryRepo(AppDbContext dbContext, ItemDataManager itemManager)
        {
            _dbContext = dbContext;
            _itemManager = itemManager;
        }

        public async Task<InventoryResult> AddItemToInventory(Session session, int itemId, int quantity)
        {
            var result = new InventoryResult();
            var inventoryCache = session.InventoryCache;
            var characterId = session.CharacterID;
            var staticItemData = _itemManager.GetItemById(itemId);
            // 1. Lấy thông tin tĩnh của item từ cache toàn cục
            
            if (staticItemData == null)
            {
                result.Message = "Item không tồn tại.";
                return result;
            }

            // 2. Xử lý gộp (stack) nếu có thể
            if (staticItemData.MaxStackSize > 1)
            {
                // Tìm các slot đã có item này và còn chỗ
                var stackableSlots = inventoryCache.Items.Values
                    .Where(i => i.ItemID == itemId && i.Quantity < staticItemData.MaxStackSize)
                    .OrderBy(i => i.Quantity); // Ưu tiên gộp vào slot ít đồ nhất

                foreach (var slot in stackableSlots)
                {
                    if (quantity <= 0) break;

                    int canAdd = staticItemData.MaxStackSize - slot.Quantity;
                    int amountToAdd = Math.Min(quantity, canAdd);

                    slot.Quantity += amountToAdd;
                    quantity -= amountToAdd;
                    result.UpdatedItems.Add(slot); // Đánh dấu item này đã được cập nhật
                }
            }

            // 3. Nếu vẫn còn item, tìm ô trống để thêm mới
            while (quantity > 0)
            {
                var freeSlot = FindFirstFreeSlot(inventoryCache);
                if (freeSlot == null)
                {
                    result.Success = result.UpdatedItems.Any(); // Thành công một phần nếu đã gộp được
                    result.Message = "Túi đồ đã đầy.";
                    return result;
                }

                int amountToCreate = Math.Min(quantity, staticItemData.MaxStackSize);

                // Tạo một entity mới trong DB để có InventoryItemID
                var newItemEntity = new InventoryItem
                {
                    InventoryID = inventoryCache.InventoryId,
                    ItemID = itemId,
                    Quantity = amountToCreate,
                    SlotNumber = freeSlot.Value,
                    IsLocked = 0,
                    AcquiredDate = DateTime.UtcNow
                };

                // Thêm vào DB trước để EF Core sinh ra ID
                _dbContext.InventoryItem.Add(newItemEntity);
                await _dbContext.SaveChangesAsync();

                // Bây giờ newItemEntity đã có InventoryItemID
                // Thêm nó vào cache của session
                inventoryCache.Items[newItemEntity.InventoryItemID] = newItemEntity;
                result.UpdatedItems.Add(newItemEntity);

                quantity -= amountToCreate;
            }

            result.Success = true;
            result.Message = "Thêm vật phẩm thành công.";
            return result;
        }

        // Hàm trợ giúp tìm ô trống
        private int? FindFirstFreeSlot(CharacterInventoryCache cache)
        {
            var occupiedSlots = new HashSet<int>(cache.Items.Values.Select(i => i.SlotNumber));
            for (int i = 0; i < cache.MaxSlots; i++)
            {
                if (!occupiedSlots.Contains(i))
                {
                    return i;
                }
            }
            return null; // Không còn ô trống
        }

        // Trong InventoryService.cs
        public async Task<InventoryResult> RemoveItemFromInventory(Session session, int inventoryItemId, int quantity)
        {
            var result = new InventoryResult();
            var inventoryCache = session.InventoryCache;

            // 1. Tìm item trong cache của người chơi
            if (!inventoryCache.Items.TryGetValue(inventoryItemId, out var itemToRemove))
            {
                result.Message = "Vật phẩm không có trong túi đồ.";
                return result;
            }

            // 2. Kiểm tra số lượng
            if (itemToRemove.Quantity < quantity)
            {
                result.Message = "Không đủ số lượng vật phẩm.";
                return result;
            }

            // 3. Cập nhật cache
            itemToRemove.Quantity -= quantity;

            if (itemToRemove.Quantity <= 0)
            {
                // Xóa hẳn khỏi cache
                inventoryCache.Items.Remove(inventoryItemId);
                result.RemovedItemIds.Add(inventoryItemId);

                // Đánh dấu để xóa trong DB khi save
                // Đây là phần nâng cao (dirty flag). Tạm thời, logic save "xóa hết thêm lại" sẽ tự xử lý.
                // Để làm đúng, bạn cần một đối tượng để theo dõi:
                // session.DirtyData.ItemsToDelete.Add(inventoryItemId);
            }
            else
            {
                // Chỉ cập nhật số lượng
                result.UpdatedItems.Add(itemToRemove);
            }

            result.Success = true;
            return result;
        }
    }
}
