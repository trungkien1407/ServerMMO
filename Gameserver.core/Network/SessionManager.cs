    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WatsonWebsocket;
    using Gameserver.core.Dto;
using Gameserver.core.Models;
using Gameserver.core.Services;
using Microsoft.Extensions.DependencyInjection;
using Gameserver.core.Repo.Context;
using Microsoft.EntityFrameworkCore;
using Gameserver.core.Manager;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Gameserver.core.Network
    {
        // Class dùng để chứa thông tin về sự kiện đăng nhập trùng
        public class DuplicateLoginEventArgs : EventArgs
        {
            public int UserId { get; }
            public Guid ExistingClientId { get; }
            public Guid NewClientId { get; }

            public DuplicateLoginEventArgs(int userId, Guid existingClientId, Guid newClientId)
            {
                UserId = userId;
                ExistingClientId = existingClientId;
                NewClientId = newClientId;
            }
        }

        public class SessionManager
        {
        private readonly Dictionary<int, DateTime> _loginCooldowns = new(); // userId → cooldown expire time


        private readonly Dictionary<int, HashSet<int>> _mapPlayers = new(); // key: MapID, value: UserIDs

        private readonly Dictionary<int, Session> _userSessions = new(); // key = UserId (int)


            private readonly object _lock = new();
            private readonly WatsonWsServer _server;
        private readonly IServiceProvider _serviceProvider;
            public SessionManager(WatsonWsServer server,IServiceProvider serviceProvider)
            {
                _server = server;
            _serviceProvider = serviceProvider;
            }

            

            public event EventHandler<DuplicateLoginEventArgs>? DuplicateLoginDetected;

            public Session CreateOrReplaceSession(int userId, Guid clientId)
            {
                lock (_lock)
                {
                    // Kiểm tra xem người dùng đã có session chưa
                    if (_userSessions.TryGetValue(userId, out var existingSession))
                    {
                        // Nếu người chơi đã đăng nhập ở nơi khác => kích hoạt sự kiện
                        if (_server.IsClientConnected(existingSession.ClientId))
                        {
                            // Thông báo phát hiện đăng nhập trùng
                            DuplicateLoginDetected?.Invoke(this, new DuplicateLoginEventArgs(
                                userId,
                                existingSession.ClientId,
                                clientId
                            ));
                        }
                        _userSessions.Remove(userId);
                    }

                    // Tạo session mới
                    var session = new Session
                    {
                        UserID = userId,
                        ClientId = clientId,
                        //ExpiresAt = DateTime.UtcNow.AddHours(1) // Ví dụ, session hết hạn sau 1 giờ
                    };
                    _userSessions[userId] = session;
                    return session;
                }
            }

        public bool CanLogin(int userId)
        {
            lock (_lock)
            {
                if (_loginCooldowns.TryGetValue(userId, out var cooldownUntil))
                {
                    if (DateTime.UtcNow < cooldownUntil)
                    {
                        return false; // còn trong thời gian chờ
                    }

                    _loginCooldowns.Remove(userId); // hết thời gian chờ rồi thì xóa luôn
                }
                return true;
            }
        }


        public Session? GetSessionByUserId(int userId)
            {
                lock (_lock)
                {
                    if (_userSessions.TryGetValue(userId, out var session))
                    {
                        return session;
                    }
                    return null;
                }
            }

            public int? GetUserIdByClientId(Guid clientId)
            {
                lock (_lock)
                {
                    return _userSessions
                        .FirstOrDefault(kvp => kvp.Value.ClientId == clientId).Key;
                }
            }
        public Guid? GetClientIdByCharacterID(int characterId)
        {
            lock (_lock)
            {
                var session = _userSessions.Values
                    .FirstOrDefault(session => session.CharacterID == characterId);

                return session?.ClientId;
            }
        }


        public Session? GetSessionByClientId(Guid clientId)
            {
                lock (_lock)
                {
                    var entry = _userSessions.FirstOrDefault(kvp => kvp.Value.ClientId == clientId);
                    return (entry.Key != 0) ? entry.Value : null;  // Check for valid key
                }
            }

        public void RemoveSessionByClientId(Guid clientId)
        {
            lock (_lock)
            {
                var entry = _userSessions.FirstOrDefault(kvp => kvp.Value.ClientId == clientId);
                if (entry.Key != 0)
                {
                    _userSessions.Remove(entry.Key);
                    _loginCooldowns[entry.Key] = DateTime.UtcNow.AddSeconds(5); // cooldown 5s
                }
            }
        }


        // Cập nhật thành int userId thay vì string userId
        public void RemoveSessionByUserId(int userId)
        {
            lock (_lock)
            {
                if (_userSessions.ContainsKey(userId))
                {
                    _userSessions.Remove(userId);
                    _loginCooldowns[userId] = DateTime.UtcNow.AddSeconds(5); // Thêm cooldown 5s
                }
            }
        }


        // Sửa kiểm tra kết nối cho userId (kiểu int)
        public bool IsUserConnected(int userId)
            {
                lock (_lock)
                {
                    return _userSessions.ContainsKey(userId) &&
                           _server.IsClientConnected(_userSessions[userId].ClientId);
                }
            }
            public bool HasAnyClientLoggedIn()
            {
                lock (_lock)
                {
                    // Kiểm tra xem có bất kỳ client nào đã đăng nhập
                    return _userSessions.Any(session => _server.IsClientConnected(session.Value.ClientId));
                }
            }
            public Dictionary<int, Session> GetSessions()
            {
                lock (_lock)
                {
                    return new Dictionary<int, Session>(_userSessions);
                }
            }

            public void CleanupExpiredSessions()
            {
                var expiredUsers = new List<int>();

                lock (_lock)
                {
                    // Cập nhật phần này để quản lý expired sessions
                    foreach (var userId in expiredUsers)
                    {
                        if (_userSessions.TryGetValue(userId, out var session))
                        {
                            if (_server.IsClientConnected(session.ClientId))
                            {
                                _server.DisconnectClient(session.ClientId);
                            }
                            _userSessions.Remove(userId);
                        }
                    }
                }
            }

        public void UpdatePlayerMap(int userId, int newMapId)
        {
            lock (_lock)
            {
                if (!_userSessions.TryGetValue(userId, out var session))
                    return;

                // Remove khỏi map cũ
                if (_mapPlayers.TryGetValue(session.MapID, out var oldMapPlayers))
                {
                    oldMapPlayers.Remove(userId);
                    if (oldMapPlayers.Count == 0)
                        _mapPlayers.Remove(session.MapID);
                }

                // Thêm vào map mới
                if (!_mapPlayers.ContainsKey(newMapId))
                {
                    _mapPlayers[newMapId] = new HashSet<int>();
                }
                _mapPlayers[newMapId].Add(userId);

                session.MapID = newMapId; // cập nhật mapId trong session
                session.Character.MapID = newMapId;
            }
        }
        public List<Session> GetPlayersInMap(int mapId)
        {
            lock (_lock)
            {
                if (_mapPlayers.TryGetValue(mapId, out var userIds))
                {
                    return userIds
                        .Where(uid => _userSessions.ContainsKey(uid))
                        .Select(uid => _userSessions[uid])
                        .ToList();
                }

                return new List<Session>();
            }
        }
        public List<Guid> GetClientIdsInMap(int mapId)
        {
            lock (_lock)
            {
                if (_mapPlayers.TryGetValue(mapId, out var userIds))
                {
                    return userIds
                        .Where(uid => _userSessions.TryGetValue(uid, out var session) && _server.IsClientConnected(session.ClientId))
                        .Select(uid => _userSessions[uid].ClientId)
                        .ToList();
                }

                return new List<Guid>();
            }
        }
        /// <summary>
        /// Lấy danh sách MapID mà đang có ít nhất 1 người chơi online
        /// </summary>
        
        public List<int> GetActiveMapIds()
        {
            lock (_lock)
            {
                return _mapPlayers
                    .Where(kvp => kvp.Value.Any(uid =>
                        _userSessions.TryGetValue(uid, out var session) &&
                        _server.IsClientConnected(session.ClientId)))
                    .Select(kvp => kvp.Key)
                    .ToList();
            }
        }
        // Trong SessionManager.cs
        public async Task SaveData(Guid clientid)
        {
            Session? session;
            lock (_lock) { session = GetSessionByClientId(clientid); }

            if (session == null || session.Character == null) return;

            // Dùng IServiceScopeFactory để đảm bảo DbContext là mới cho mỗi lần save
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Bắt đầu một transaction để đảm bảo tất cả đều thành công hoặc thất bại
            using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                // 1. Lưu dữ liệu Character
                var characterData = await dbContext.Characters.FindAsync(session.Character.CharacterID);
                if (characterData != null)
                {
                    characterData.Level = session.Character.Level;
                    characterData.Exp = session.Character.Exp;
                    characterData.Gold = session.Character.Gold;
                    characterData.X = session.Character.X;
                    characterData.Y = session.Character.Y;
                    characterData.BodyID = session.Character.BodyID;
                    characterData.PantID = session.Character.PantID;
                    characterData.MapID = session.Character.MapID;
                    characterData.CurrentHealth = session.Character.CurrentHealth;
                    characterData.CurrentMana = session.Character.CurrentMana;
                    characterData.SkillPoints = session.Character.SkillPoints;
                    characterData.Strength = session.Character.Strength;
                    characterData.Dexterity = session.Character.Dexterity;
                    characterData.Intelligence = session.Character.Intelligence;
                    characterData.Health = session.Character.Health;
                    characterData.Mana = session.Character.Mana;
                }

                // 2. Lưu dữ liệu Equipment (ĐÃ SỬA LỖI)
                var equipmentData = await dbContext.CharacterEquipment
                                                   .FirstOrDefaultAsync(eq => eq.CharacterID == session.Character.CharacterID);
                if (equipmentData == null)
                {
                    equipmentData = new CharacterEquipment { CharacterID = session.Character.CharacterID };
                    dbContext.CharacterEquipment.Add(equipmentData);
                }
                equipmentData.WeaponSlot = session.EquipmentCache.WeaponSlot_InventoryItemID;
                equipmentData.SlotAo = session.EquipmentCache.AoSlot_InventoryItemID;
                equipmentData.SlotQuan = session.EquipmentCache.QuanSlot_InventoryItemID;
                equipmentData.SlotGiay = session.EquipmentCache.GiaySlot_InventoryItemID;
                equipmentData.SlotGangTay = session.EquipmentCache.GangTaySlot_InventoryItemID;
                equipmentData.SlotBua = session.EquipmentCache.BuaSlot_InventoryItemID;

                // 3. Lưu dữ liệu Inventory (TỐI ƯU HÓA)
                var dbItems = await dbContext.InventoryItem
                                             .Where(i => i.InventoryID == session.InventoryCache.InventoryId)
                                             .ToDictionaryAsync(i => i.InventoryItemID); // Dùng Dictionary để tra cứu nhanh

                var itemsToRemove = new List<InventoryItem>(dbItems.Values); // Giả định tất cả item cũ sẽ bị xóa

                foreach (var cachedItem in session.InventoryCache.Items.Values)
                {
                    // Trường hợp 1: Item đã tồn tại trong DB -> Cập nhật
                    if (cachedItem.InventoryItemID > 0 && dbItems.TryGetValue(cachedItem.InventoryItemID, out var dbItem))
                    {
                        // Cập nhật các thuộc tính nếu có thay đổi
                        dbItem.Quantity = cachedItem.Quantity;
                        dbItem.SlotNumber = cachedItem.SlotNumber;
                        dbItem.IsLocked = cachedItem.IsLocked;
                        dbItem.Equipped = cachedItem.Equipped;


                        // ... các thuộc tính khác

                        itemsToRemove.Remove(dbItem); // Item này vẫn tồn tại, không xóa nó
                    }
                    // Trường hợp 2: Item mới được thêm vào túi đồ -> Thêm mới
                    else
                    {
                        // Phải tạo một instance mới vì cachedItem không được theo dõi bởi DbContext này
                        var newItemEntity = new InventoryItem
                        {
                            // Không set InventoryItemID, để DB tự tạo
                            InventoryID = session.InventoryCache.InventoryId,
                            ItemID = cachedItem.ItemID,
                            Quantity = cachedItem.Quantity,
                            SlotNumber = cachedItem.SlotNumber,
                            IsLocked = cachedItem.IsLocked,
                            AcquiredDate = cachedItem.AcquiredDate
                        };
                        dbContext.InventoryItem.Add(newItemEntity);
                    }
                }

                // Trường hợp 3: Xóa những item không còn trong cache nữa
                if (itemsToRemove.Count > 0)
                {
                    dbContext.InventoryItem.RemoveRange(itemsToRemove);
                }

                // Lưu tất cả thay đổi vào DB
                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Lỗi khi lưu dữ liệu cho character {session.CharacterID}: {ex}");
                // Có thể log chi tiết hơn vào file hoặc hệ thống log
            }
        }

        public async Task Autosave()
        {
            List<Guid> clientIds;

            // Copy danh sách trong lock, nhưng xử lý bên ngoài
            lock (_lock)
            {
                clientIds = _userSessions.Values
                    .Where(session => _server.IsClientConnected(session.ClientId))
                    .Select(session => session.ClientId)
                    .ToList();
            }
            if (clientIds.Count <= 0) return;

            foreach (var clientId in clientIds)
            {
                try
                {
                    await SaveData(clientId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi autosave cho client {clientId}: {ex}");
                }
            }
        }


        public async Task SendToastNotification(Guid clientId, string message, string type)
        {
            var notificationMessage = new BaseMessage
            {
                Action = "show_toast",
                Data = JObject.FromObject(new { text = message, type = type })
            };

            // _server là WatsonWsServer đã được inject vào SessionManager
            if (_server.IsClientConnected(clientId))
            {
                await _server.SendAsync(clientId, JsonConvert.SerializeObject(notificationMessage));
            }
        }

        /// <summary>
        /// Gửi cập nhật đầy đủ về túi đồ và trang bị cho client.
        /// </summary>
        public async Task SendFullInventoryUpdate(Session session)
        {
            if (session == null || !_server.IsClientConnected(session.ClientId)) return;

            // Chuyển đổi cache thành các đối tượng DTO để gửi đi
            var inventoryDto = new InventoryDto
            {
                InventoryId = session.InventoryCache.InventoryId,
                MaxSlots = session.InventoryCache.MaxSlots,
                Items = session.InventoryCache.Items.Values.ToDictionary(
                    item => item.InventoryItemID,
                    item => new InventoryItemDto
                    {
                        InventoryItemID = item.InventoryItemID,
                        ItemID = item.ItemID,
                        Quantity = item.Quantity,
                        SlotNumber = item.SlotNumber,
                        Equiped = item.Equipped,
                        IsLocked = item.IsLocked
                    }
                )
            };

            var message = new BaseMessage
            {
                Action = "get_inventory_equip",
                Data = JObject.FromObject(new
                {
                    inventory = inventoryDto,
                    equip = session.EquipmentCache
                })
            };
            await _server.SendAsync(session.ClientId, JsonConvert.SerializeObject(message));
        }

        /// <summary>
        /// Thêm một vật phẩm vào túi đồ của người chơi, xử lý xếp chồng và gửi thông báo.
        /// </summary>
        /// <returns>True nếu thêm thành công, False nếu thất bại (ví dụ: túi đồ đầy).</returns>
        public async Task<bool> AddItemToInventory(Session session, int itemId, int quantity)
        {
            if (session == null || session.Character == null) return false;

            // Cần truy cập ItemDataManager, nó nên được inject vào SessionManager
            using var scope = _serviceProvider.CreateScope();
            var itemDataManager = scope.ServiceProvider.GetRequiredService<ItemDataManager>();

            var itemData = itemDataManager.GetItemById(itemId);
            if (itemData == null)
            {
                Console.WriteLine($"[AddItemToInventory] Attempted to add non-existent item with ID: {itemId}");
                return false;
            }

            var inventory = session.InventoryCache;

            // Logic thêm vật phẩm (có thể cải tiến thêm)
            // 1. Nếu vật phẩm có thể xếp chồng, tìm stack hiện có
            if (itemData.MaxStackSize >1) // Giả sử CachedItemData có thuộc tính này
            {
                var existingStack = inventory.Items.Values
                    .FirstOrDefault(i => i.ItemID == itemId && i.Quantity < itemData.MaxStackSize); // Giả sử có MaxStack

                if (existingStack != null)
                {
                    existingStack.Quantity += quantity;
                    // TODO: Xử lý trường hợp số lượng vượt quá MaxStack
                }
                else
                {
                    // Không có stack nào có thể thêm vào, tạo stack mới
                    if (!CreateNewItemInInventory(session, itemId, quantity))
                    {
                        await SendToastNotification(session.ClientId, "Túi đồ đã đầy!", "error");
                        return false;
                    }
                }
            }
            else // Vật phẩm không thể xếp chồng, luôn tạo mới
            {
                // Kiểm tra xem có đủ chỗ trống cho tất cả các item không
                if (inventory.Items.Count + quantity > inventory.MaxSlots)
                {
                    await SendToastNotification(session.ClientId, "Túi đồ đã đầy!", "error");
                    return false;
                }

                for (int i = 0; i < quantity; i++)
                {
                    CreateNewItemInInventory(session, itemId, 1);
                }
            }

            // Gửi thông báo thành công cho client
            string messageText = $"Bạn nhận được [{itemData.Name}] x{quantity}";
            await SendToastNotification(session.ClientId, messageText, "item_received");

            // Gửi cập nhật toàn bộ túi đồ để client vẽ lại UI
            await SendFullInventoryUpdate(session);

            return true;
        }

        /// <summary>
        /// Phương thức phụ trợ để tạo một vật phẩm mới trong slot trống.
        /// </summary>
        /// <returns>True nếu tạo thành công, False nếu túi đồ đầy.</returns>
        private bool CreateNewItemInInventory(Session session, int itemId, int quantity)
        {
            var inventory = session.InventoryCache;
            if (inventory.Items.Count >= inventory.MaxSlots)
            {
                // Có thể gửi thông báo túi đầy ở đây nếu cần
                return false;
            }

            // --- LOGIC TÌM SLOT TRỐNG (giữ nguyên) ---
            int firstEmptySlot = 0;
            var usedSlots = new HashSet<int>(inventory.Items.Values.Select(i => i.SlotNumber));
            while (usedSlots.Contains(firstEmptySlot))
            {
                firstEmptySlot++;
            }
            int maxRealId = 0;
            var realItems = inventory.Items.Keys.Where(k => k > 0);
            if (realItems.Any())
            {
                maxRealId = realItems.Max();
            }
            int temporaryId = maxRealId + 1;


            // TẠO ITEM MỚI VỚI ID TẠM THỜI
            var newItem = new InventoryItem
            {
                InventoryItemID = temporaryId,
                InventoryID = inventory.InventoryId,
                ItemID = itemId,
                Quantity = quantity,
                SlotNumber = firstEmptySlot,
                Equipped = 0,
                IsLocked = 0,
                AcquiredDate = DateTime.UtcNow,
              
            };

            // THÊM VÀO CACHE VÀ HÀNG ĐỢI
            // Thêm vào cache để có thể sử dụng ngay
            inventory.Items.Add(newItem.InventoryItemID, newItem);

            // Đưa vào hàng đợi để background worker lưu vào DB sau
            // Giả sử bạn có một service cho việc này
            // _databasePersistenceService.EnqueueInventorySave(newItem);

            Console.WriteLine($"[Inventory] Created temporary item. ItemID: {itemId}, TempInventoryID: {temporaryId}, Slot: {firstEmptySlot}");

            return true;
        }

    }
}
