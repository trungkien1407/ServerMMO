using Gameserver.core.Dto;
using Gameserver.core.Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace Gameserver.core.Handler
{
    public class GetInventoryHandle : IMessageHandler
    {
        public string Action => "get_inventory_equip";
        private readonly SessionManager _sessionManager;
        public GetInventoryHandle(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }


        public async Task Handle(Guid clientId, JObject data, WatsonWsServer server)
        {
            var session = _sessionManager.GetSessionByClientId(clientId);
            if (session == null) return; // Luôn kiểm tra session tồn tại

            var inventoryCache = session.InventoryCache;
            var equipmentCache = session.EquipmentCache; // Giả sử EquipmentCache đã là một DTO/class đơn giản

            // === BẮT ĐẦU ÁNH XẠ TỪ MODEL SANG DTO ===

            // 1. Tạo DTO cho túi đồ
            var inventoryDto = new InventoryDto
            {
                InventoryId = inventoryCache.InventoryId,
                MaxSlots = inventoryCache.MaxSlots
            };

            // 2. Duyệt qua các item trong cache và tạo DTO cho từng item
            foreach (var itemModel in inventoryCache.Items.Values)
            {
                var itemDto = new InventoryItemDto
                {
                    InventoryItemID = itemModel.InventoryItemID,
                    ItemID = itemModel.ItemID,
                    Quantity = itemModel.Quantity,
                    SlotNumber = itemModel.SlotNumber,
                    IsLocked = itemModel.IsLocked
                };
                inventoryDto.Items.Add(itemDto.InventoryItemID, itemDto);
            }

            // === KẾT THÚC ÁNH XẠ ===

            var message = new BaseMessage
            {
                Action = "get_inventory_equip",
                // 3. Sử dụng các đối tượng DTO đã "sạch" để tạo message
                Data = JObject.FromObject(new
                {
                    inventory = inventoryDto, // Sử dụng inventoryDto thay vì inventoryCache
                    equip = equipmentCache
                })
            };

            // Bây giờ việc serialize sẽ không còn lỗi
            await server.SendAsync(clientId, JsonConvert.SerializeObject(message));
        }
    }
}