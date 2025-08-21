using Gameserver.core.Network;
using Gameserver.core.Models;
using Gameserver.core.Manager;
using Gameserver.core.Dto;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace Gameserver.core.Handler
{
    /// <summary>
    /// Xử lý yêu cầu "sử dụng vật phẩm" từ client.
    /// Nó xử lý logic trên cache, lưu vào DB, gửi lại cập nhật cho người chơi,
    /// và broadcast các thay đổi cần thiết cho người chơi khác.
    /// </summary>
    public class UseItemHandle : IMessageHandler
    {
        public string Action => "use_item";
        private readonly SessionManager _sessionManager;
        private readonly IServiceProvider _serviceProvider;
        private readonly ItemDataManager _itemDataManager;

        public UseItemHandle(SessionManager sessionManager, IServiceProvider serviceProvider, ItemDataManager itemDataManager)
        {
            _sessionManager = sessionManager;
            _serviceProvider = serviceProvider;
            _itemDataManager = itemDataManager;
        }

        public async Task Handle(Guid clientId, JObject data, WatsonWsServer server)
        {
            var session = _sessionManager.GetSessionByClientId(clientId);
            if (session == null || session.Character == null) return;

            if (!data.TryGetValue("inventoryItemId", out var inventoryItemIdToken)) return;
            int inventoryItemId = inventoryItemIdToken.Value<int>();

            if (!session.InventoryCache.Items.TryGetValue(inventoryItemId, out var itemToUse))
            {
                Console.WriteLine($"[UseItemHandle] Player {session.Character.Name} tried to use item not in cache (ID: {inventoryItemId}).");
                return;
            }

            var cachedItemData = _itemDataManager.GetItemById(itemToUse.ItemID);
            if (cachedItemData == null) return;

            bool dataChanged = false;
            switch (cachedItemData.Type.ToLower())
            {
                case "equipment":
                    // Nếu vật phẩm người chơi nhấn vào ĐÃ ĐƯỢC trang bị => Tháo ra
                    if (itemToUse.Equipped == 1)
                    {
                        dataChanged = HandleUnequipItem(session, itemToUse, cachedItemData);
                    }
                    // Nếu vật phẩm người chơi nhấn vào CHƯA ĐƯỢC trang bị => Mặc vào (và thay thế đồ cũ nếu có)
                    else
                    {
                        dataChanged = HandleEquipItem(session, itemToUse, cachedItemData);
                    }
                    break;
                case "hp":
                    dataChanged = HandleConsumeItem(session, itemToUse, cachedItemData);
                    break;
            }


            if (dataChanged)
            {
                // BƯỚC 1: LƯU DỮ LIỆU
               // await _sessionManager.SaveData(clientId);

                // BƯỚC 2: CẬP NHẬT NGOẠI HÌNH (LOGIC CŨ)
                // Cập nhật các ID ngoại hình trực tiếp trong cache của session
                switch (itemToUse.ItemID)
                {
                    case 110: session.Character.BodyID = 2; break;
                    case 112: session.Character.BodyID = 3; break;
                    case 113: session.Character.PantID = 3; break;
                    case 115: session.Character.PantID = 2; break;
                        // Thêm logic để reset về mặc định khi tháo đồ nếu cần
                }

                // BƯỚC 3: GỬI LẠI CẬP NHẬT ĐẦY ĐỦ CHO CHÍNH NGƯỜI CHƠI
                await SendFullInventoryUpdate(session, server);

                // BƯỚC 4: BROADCAST THAY ĐỔI CHO NHỮNG NGƯỜI CHƠI KHÁC
                await BroadcastChanges(clientId, session, cachedItemData.Type, server);
            }
        }

        #region Action Handlers

        private bool HandleEquipItem(Session session, InventoryItem newItem, CachedItemData newItemData)
        {
            if (session.Character.Level < newItemData.RequiredLevel) return false;

            var equipmentCache = session.EquipmentCache;
            var inventoryCache = session.InventoryCache;
            var character = session.Character;

            // --- LOGIC THAY THẾ BẮT ĐẦU TỪ ĐÂY ---

            // BƯỚC 1: XÁC ĐỊNH VẬT PHẨM CŨ (NẾU CÓ) TRONG SLOT TƯƠNG ỨNG
            int? oldItemInventoryId = newItemData.SlotType switch
            {
                0 => equipmentCache.WeaponSlot_InventoryItemID,
                1 => equipmentCache.AoSlot_InventoryItemID,
                2 => equipmentCache.QuanSlot_InventoryItemID,
                3 => equipmentCache.GiaySlot_InventoryItemID,
                4 => equipmentCache.GangTaySlot_InventoryItemID,
                5 => equipmentCache.BuaSlot_InventoryItemID,
                _ => null
            };
            if (newItemData.SlotType > 5) return false;

            // BƯỚC 2: NẾU CÓ VẬT PHẨM CŨ, "THÁO" NÓ RA TRƯỚC
            if (oldItemInventoryId.HasValue && inventoryCache.Items.TryGetValue(oldItemInventoryId.Value, out var oldItem))
            {
                var oldItemData = _itemDataManager.GetItemById(oldItem.ItemID);
                if (oldItemData != null)
                {
                    // Trừ đi chỉ số của vật phẩm cũ khỏi chỉ số tổng của nhân vật
                    character.Health -= oldItemData.HPBonus ?? 0;
                    character.Strength -= oldItemData.AttackBonus ?? 0;
                    // (Thêm các chỉ số khác ở đây...)
                }
                // Đánh dấu vật phẩm cũ là không còn được trang bị
                oldItem.Equipped = 0;
            }

            // BƯỚC 3: TRANG BỊ VẬT PHẨM MỚI
            // Cộng chỉ số của vật phẩm mới vào chỉ số tổng của nhân vật
            character.Health += newItemData.HPBonus ?? 0;
            character.Strength += newItemData.AttackBonus ?? 0;
            // (Thêm các chỉ số khác ở đây...)

            // Đánh dấu vật phẩm mới là ĐÃ được trang bị
            newItem.Equipped = 1;

            // Cập nhật slot trong EquipmentCache để trỏ tới ID của vật phẩm MỚI
            switch (newItemData.SlotType)
            {
                case 0: equipmentCache.WeaponSlot_InventoryItemID = newItem.InventoryItemID; break;
                case 1: equipmentCache.AoSlot_InventoryItemID = newItem.InventoryItemID; break;
                case 2: equipmentCache.QuanSlot_InventoryItemID = newItem.InventoryItemID; break;
                case 3: equipmentCache.GiaySlot_InventoryItemID = newItem.InventoryItemID; break;
                case 4: equipmentCache.GangTaySlot_InventoryItemID = newItem.InventoryItemID; break;
                case 5: equipmentCache.BuaSlot_InventoryItemID = newItem.InventoryItemID; break;
            }

            // BƯỚC 4: ĐIỀU CHỈNH MÁU HIỆN TẠI
            // Đảm bảo máu hiện tại không vượt quá máu tối đa sau khi thay đổi trang bị
            if (character.CurrentHealth > character.Health)
            {
                character.CurrentHealth = character.Health;
            }

            return true;
        }

        /// <summary>
        /// Xử lý logic khi THÁO một vật phẩm đang trang bị (không thay thế bằng cái mới).
        /// </summary>
        private bool HandleUnequipItem(Session session, InventoryItem itemToUnequip, CachedItemData itemData)
        {
            var equipmentCache = session.EquipmentCache;
            var character = session.Character;

            // Trừ chỉ số của vật phẩm bị tháo
            character.Health -= itemData.HPBonus ?? 0;
            character.Strength -= itemData.AttackBonus ?? 0;
            // (Thêm các chỉ số khác ở đây...)

            // Cập nhật trạng thái
            itemToUnequip.Equipped = 0;

            // Dọn dẹp slot trang bị, gán về null
            switch (itemData.SlotType)
            {
                case 0: if (equipmentCache.WeaponSlot_InventoryItemID == itemToUnequip.InventoryItemID) equipmentCache.WeaponSlot_InventoryItemID = null; break;
                case 1: if (equipmentCache.AoSlot_InventoryItemID == itemToUnequip.InventoryItemID) equipmentCache.AoSlot_InventoryItemID = null; break;
                case 2: if (equipmentCache.QuanSlot_InventoryItemID == itemToUnequip.InventoryItemID) equipmentCache.QuanSlot_InventoryItemID = null; break;
                case 3: if (equipmentCache.GiaySlot_InventoryItemID == itemToUnequip.InventoryItemID) equipmentCache.GiaySlot_InventoryItemID = null; break;
                case 4: if (equipmentCache.GangTaySlot_InventoryItemID == itemToUnequip.InventoryItemID) equipmentCache.GangTaySlot_InventoryItemID = null; break;
                case 5: if (equipmentCache.BuaSlot_InventoryItemID == itemToUnequip.InventoryItemID) equipmentCache.BuaSlot_InventoryItemID = null; break;
                default: return false;
            }

            // Điều chỉnh Máu hiện tại
            if (character.CurrentHealth > character.Health)
            {
                character.CurrentHealth = character.Health;
            }

            return true;
        }
        private bool HandleConsumeItem(Session session, InventoryItem itemToConsume, CachedItemData cachedData)
        {
            var characterCache = session.Character;

            switch (itemToConsume.ItemID)
            {
                case 103: characterCache.CurrentHealth += 100;
                    break;
                case 104: characterCache.CurrentMana += 100;
                    break;
            }

            Console.WriteLine($"currenthp, currentmana {characterCache.CurrentHealth},{characterCache.CurrentMana}");
            if(characterCache.CurrentHealth >= characterCache.Health)  characterCache.CurrentHealth = characterCache.Health;
            if(characterCache.CurrentMana >= characterCache.CurrentMana)  characterCache.CurrentMana = characterCache.Mana;
            itemToConsume.Quantity -= 1;
            if (itemToConsume.Quantity <= 0)
            {
                session.InventoryCache.Items.Remove(itemToConsume.InventoryItemID);
            }
            return true;
        }

        #endregion

        #region Helper Methods

        private async Task SendFullInventoryUpdate(Session session, WatsonWsServer server)
        {
            var inventoryCache = session.InventoryCache;
            var equipmentCache = session.EquipmentCache; // Giả sử EquipmentCache đã là một DTO/class đơn giản
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
                    Equiped = itemModel.Equipped,
                    IsLocked = itemModel.IsLocked
                };
                inventoryDto.Items.Add(itemDto.InventoryItemID, itemDto);
            }
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
            await server.SendAsync(session.ClientId, JsonConvert.SerializeObject(message));
        }

        /// <summary>
        /// Gửi thông báo về những thay đổi có thể nhìn thấy cho những người chơi khác.
        /// Sử dụng Task.WhenAll để gửi đồng thời.
        /// </summary>
        private async Task BroadcastChanges(Guid sourceClientId, Session sourceSession, string itemType, WatsonWsServer server)
        {
            BaseMessage? messageToBroadcast = null;
            string itemCategory = itemType.ToLower();

            if (itemCategory == "equipment")
            {
                messageToBroadcast = new BaseMessage
                {
                    Action = "update_sprite",
                    Data = JObject.FromObject(new
                    {
                        characterid = sourceSession.Character.CharacterID,
                        character = sourceSession.Character,
                        body = sourceSession.Character.BodyID,
                        pant = sourceSession.Character.PantID, // Sửa 'paint' thành 'pant'
                    })
                };
            }
            else if (itemCategory == "hp" || itemCategory == "consumable")
            {
                messageToBroadcast = new BaseMessage
                {
                    Action = "update_character_stats",
                    Data = JObject.FromObject(new
                    {
                        characterId = sourceSession.Character.CharacterID,
                        currentHealth = sourceSession.Character.CurrentHealth,
                        currentMana = sourceSession.Character.CurrentMana,
                    })
                };
            }

            if (messageToBroadcast != null)
            {
                // Lấy danh sách client khác trong map
                List<Guid> otherClients = _sessionManager.GetClientIdsInMap(sourceSession.MapID);
                                                         

                if (otherClients.Any())
                {
                    string payload = JsonConvert.SerializeObject(messageToBroadcast);

                    // *** THAY ĐỔI Ở ĐÂY: SỬ DỤNG TASK.WHENALL ***
                    // Tạo một danh sách các tác vụ gửi tin (List of Tasks)
                    var sendTasks = otherClients.Select(clientId => server.SendAsync(clientId, payload));

                    // Chờ cho tất cả các tác vụ gửi tin hoàn thành
                    await Task.WhenAll(sendTasks);
                }
            }
        }

        #endregion
    }
}