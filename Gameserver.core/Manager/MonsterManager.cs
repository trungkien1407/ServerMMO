using Gameserver.core.Dto;
using Gameserver.core.Manager;
using Gameserver.core.Models;
using Gameserver.core.Network;
using Gameserver.core.Repo.Interfaces;
using Gameserver.core.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Json.Nodes;
using WatsonWebsocket;

public class MonsterManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SessionManager _sessionManager;
    private readonly WatsonWsServer _watsonWsServer;
    private readonly QuestManager _questManager;
    private readonly Dictionary<int, List<MonsterRuntime>> _cacheMonstersByMap = new();
    private readonly Dictionary<int, List<GroundItem>> _groundItemsByMap = new();

    private readonly Dictionary<int, bool> _mapInitialized = new(); // Track nếu map đã init
    private readonly object _lock = new();

    // Các hằng số Action để gửi message

    public static class GroundItemActions
    {
        public const string ITEM_SPAWNED = "ground_item_spawned";
        public const string ITEM_PICKED_UP = "ground_item_picked_up";
        public const string ITEM_DESPAWN = "ground_item_despawn";
    }
    public static class MonsterActions
    {
        public const string MONSTER_SPAWN = "monster_spawn";
        public const string MONSTER_MOVE = "monster_move";
        public const string MONSTER_ATTACK = "monster_attack";
        public const string MONSTER_TAKE_DAMAGE = "monster_take_damage";
        public const string MONSTER_DEATH = "monster_death";
        public const string MONSTER_RESPAWN = "monster_respawn";
        //public const string PLAYER_TAKE_DAMAGE = "player_take_damage";
        public const string PLAYER_DEATH = "player_death";
    }

    public MonsterManager(IServiceProvider serviceProvider, SessionManager sessionManager, WatsonWsServer watsonWsServer,QuestManager questManager)
    {
        _serviceProvider = serviceProvider;
        _sessionManager = sessionManager;
        _watsonWsServer = watsonWsServer;
        _questManager = questManager;
    }

    /// <summary>
    /// Load và cache các monster trên map, CHỈ tạo mới nếu chưa có
    /// </summary>
    public async Task<List<MonsterRuntime>> LoadMonstersForMapAsync(int mapId)
    {
        lock (_lock)
        {
            // Nếu đã có monsters và đã init thì return luôn
            if (_cacheMonstersByMap.TryGetValue(mapId, out var cachedMonsters) &&
                _mapInitialized.ContainsKey(mapId))
            {
                return new List<MonsterRuntime>(cachedMonsters); // Return copy để thread-safe
            }
        }

        // Nếu chưa có thì load từ DB
        using var scope = _serviceProvider.CreateScope();
        var monsterRepo = scope.ServiceProvider.GetRequiredService<IMonsterRepo>();

        var spawns = await monsterRepo.GetMonsterSpawnsByMapIdAsync(mapId);
        var monsters = new List<MonsterRuntime>();

        foreach (var spawn in spawns)
        {
            var monster = new MonsterRuntime(spawn.Monster, spawn, 0);
            monsters.Add(monster);
        }

        lock (_lock)
        {
            // CHỈ set nếu chưa có hoặc chưa init
            if (!_cacheMonstersByMap.ContainsKey(mapId) || !_mapInitialized.ContainsKey(mapId))
            {
                _cacheMonstersByMap[mapId] = monsters;
                _mapInitialized[mapId] = true;
             //   Console.WriteLine($"[MonsterManager] Initialized {monsters.Count} monsters for map {mapId}");
            }
        }

        lock (_lock)
        {
            return new List<MonsterRuntime>(_cacheMonstersByMap[mapId]);
        }
    }

    /// <summary>
    /// Lấy monster từ cache (copy ra để tránh khóa) - CHỈ dùng để READ
    /// </summary>
    public List<MonsterRuntime> GetMonstersFromCache(int mapId)
    {
        lock (_lock)
        {
            return _cacheMonstersByMap.TryGetValue(mapId, out var monsters)
                ? new List<MonsterRuntime>(monsters) // Return copy
                : new List<MonsterRuntime>();
        }
    }

    /// <summary>
    /// Lấy trực tiếp reference tới monsters (CHỈ dùng trong tick logic)
    /// </summary>
    private List<MonsterRuntime>? GetMonstersReference(int mapId)
    {
        lock (_lock)
        {
            return _cacheMonstersByMap.TryGetValue(mapId, out var monsters) ? monsters : null;
        }
    }

    /// <summary>
    /// Tick logic cho tất cả monster trên các map đang có player
    /// </summary>
    public async Task TickMonsters()
    {
        var activeMapIds = _sessionManager.GetActiveMapIds();

        foreach (var mapId in activeMapIds)
        {
            // Lấy reference trực tiếp để modify
            var monsters = GetMonstersReference(mapId);
            if (monsters == null) continue;

            var playersInMap = _sessionManager.GetPlayersInMap(mapId);

            // Dictionary để gom các message theo action
            var batchedMessages = new Dictionary<string, JArray>
            {
                [MonsterActions.MONSTER_MOVE] = new JArray(),
                [MonsterActions.MONSTER_ATTACK] = new JArray(),
                [MonsterActions.MONSTER_TAKE_DAMAGE] = new JArray(),
                [MonsterActions.MONSTER_DEATH] = new JArray(),
                [MonsterActions.MONSTER_RESPAWN] = new JArray(),
           //     [MonsterActions.PLAYER_TAKE_DAMAGE] = new JArray(),
                [MonsterActions.PLAYER_DEATH] = new JArray()
            };

            // Sử dụng lock khi process để tránh xung đột
            List<MonsterRuntime> monstersToProcess;
            lock (_lock)
            {
                monstersToProcess = new List<MonsterRuntime>(monsters);
            }

            // Xử lý logic cho từng monster
            foreach (var monster in monstersToProcess)
            {
                await ProcessMonsterLogicBatched(mapId, monster, playersInMap, batchedMessages);
            }

            // Broadcast các message đã gom
            await BroadcastBatchedMessages(mapId, batchedMessages);
            await ProcessGroundItemsTick(mapId);
        }
        
    }

    /// <summary>
    /// Xử lý logic cho monster và gom message vào batch
    /// </summary>
    private async Task ProcessMonsterLogicBatched(int mapId, MonsterRuntime monster, List<Session> playersInMap, Dictionary<string, JArray> batchedMessages)
    {
        // 1. Respawn nếu cần
        if (!monster.IsAlive)
        {
            if (monster.ReadyToRespawn)
            {
                monster.Respawn();
            //    Console.WriteLine($"[SERVER] Monster {monster.MonsterName} respawned at ({monster.X}, {monster.Y})");

                // Thêm vào batch respawn
                batchedMessages[MonsterActions.MONSTER_RESPAWN].Add(new JObject
                {
                    ["spawnId"] = monster.SpawnID,
                    ["x"] = monster.X,
                    ["y"] = monster.Y,
                    ["currentHp"] = monster.CurrentHP,
                    ["monsterImg"] = monster.MonsterImg,
                    ["monsterName"] = monster.MonsterName,

                });
            }
            return;
        }

        // 2. Di chuyển - LƯU LẠI VỊ TRÍ CŨ TRƯỚC KHI MOVE
        var oldX = monster.X;
        var oldY = monster.Y;

        // GỌI MOVE METHOD
        monster.Move();

        // CHỈ log khi có thay đổi đáng kể
        var hasSignificantMove = Math.Abs(oldX - monster.X) > 0.1f || Math.Abs(oldY - monster.Y) > 0.1f;

        if (hasSignificantMove)
        {
           Console.WriteLine($"[Tick] Monster ID {monster.SpawnID}, Name '{monster.MonsterName}', Position: ({monster.X:F2},{monster.Y:F2})");

            batchedMessages[MonsterActions.MONSTER_MOVE].Add(new JObject
            {
                ["MonsterId"] = monster.MonsterID,
                ["spawnId"] = monster.SpawnID,
                ["x"] = Math.Round(monster.X, 2), // Làm tròn để giảm data
                ["y"] = Math.Round(monster.Y, 2),
                ["oldX"] = Math.Round(oldX, 2),
                ["oldY"] = Math.Round(oldY, 2)
            });
        }

        // 3. Tấn công nếu có thể
        if (monster.CanAttack())
        {
            var targetPlayer = FindNearestPlayerInRange(monster, playersInMap);
            if (targetPlayer != null)
            {
                int damage = monster.Attack();
                if (damage > 0)
                {
                  //  Console.WriteLine($"[SERVER] Monster {monster.SpawnID} attacks player {targetPlayer.Character.CharacterID} for {damage} damage!");

                    // Thêm vào batch attack
                    batchedMessages[MonsterActions.MONSTER_ATTACK].Add(new JObject
                    {
                        ["spawnId"] = monster.SpawnID,
                        ["targetPlayerId"] = targetPlayer.Character.CharacterID,
                        ["damage"] = damage
                    });

                    // Xử lý player nhận damage
                    ProcessPlayerTakeDamageBatched(mapId, targetPlayer, damage, batchedMessages);
                }
            }
        }
    }

    /// <summary>
    /// Xử lý player nhận damage từ monster và thêm vào batch
    /// </summary>
    private void ProcessPlayerTakeDamageBatched(int mapId, Session targetPlayer, int damage, Dictionary<string, JArray> batchedMessages)
    {
        targetPlayer.Character.CurrentHealth -= damage;
        if (targetPlayer.Character.CurrentHealth < 0)
            targetPlayer.Character.CurrentHealth = 0;

        Console.WriteLine($"[SERVER] Player {targetPlayer.Character.CharacterID} took {damage} damage. HP: {targetPlayer.Character.CurrentHealth}/{targetPlayer.Character.Health}");

        // Thêm vào batch player take damage
        //batchedMessages[MonsterActions.PLAYER_TAKE_DAMAGE].Add(new JObject
        //{
        //    ["playerId"] = targetPlayer.Character.CharacterID,
        //    ["damage"] = damage,
        //    ["currentHp"] = targetPlayer.Character.CurrentHealth
        //});

        if (targetPlayer.Character.CurrentHealth <= 0)
        {
            Console.WriteLine($"[SERVER] Player {targetPlayer.Character.CharacterID} has died!");

            // Thêm vào batch player death
            batchedMessages[MonsterActions.PLAYER_DEATH].Add(new JObject
            {
                ["playerId"] = targetPlayer.Character.CharacterID,
                ["playerName"] = targetPlayer.Character.Name,
                ["deathTime"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }
    }

    /// <summary>
    /// Broadcast tất cả các message đã gom
    /// </summary>
    private async Task BroadcastBatchedMessages(int mapId, Dictionary<string, JArray> batchedMessages)
    {
        var tasks = new List<Task>();

        foreach (var kvp in batchedMessages)
        {
            var action = kvp.Key;
            var dataArray = kvp.Value;

            // Chỉ broadcast nếu có data
            if (dataArray.Count > 0)
            {
                var message = new BaseMessage
                {
                    Action = action,
                    Data = new JObject
                    {
                        ["items"] = dataArray,
                        ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    }
                };

                tasks.Add(BroadcastToMap(mapId, message));
            }
        }

        // Chạy song song tất cả broadcast
        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }

    /// <summary>
    /// Xử lý player tấn công monster
    /// </summary>
        public async Task ProcessPlayerAttackMonster(int mapId, int playerId, int spawnId, int damage, Session session, bool facingRight)
        {
            var monsters = GetMonstersReference(mapId);
            if (monsters == null) return;

            MonsterRuntime? monster;
            lock (_lock)
            {
                monster = monsters.FirstOrDefault(m => m.SpawnID == spawnId);
            }

            if (monster == null || !monster.IsAlive)
                return;

            Console.WriteLine($"[SERVER] Player {playerId} attacks monster {monster.MonsterName} for {damage} damage");

            monster.TakeDamage(damage);
            var expGained = CalculateExpGain(monster.Level);
             bool leveled =  session.Character.AddExp(expGained);
            float expPercent = session.Character.ExpPercentToNextLevel;
            var respone = new BaseMessage
            {
                Action = "percent",
                Data = new JObject
                {
                    ["percent"] = expPercent,

                }
            };

            await _watsonWsServer.SendAsync(session.ClientId, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(respone)));
        if (leveled)
            {
                var message = new BaseMessage
                {
                    Action = "level_up",
                    Data = new JObject
                    {
                        ["character"] = JObject.FromObject(session.Character)
                    }
                };
                var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
              await  _watsonWsServer.SendAsync(session.ClientId, payload);
            }

       
            await BroadcastMonsterTakeDamage(mapId, monster, damage, playerId);


        if (!monster.IsAlive)
        {
            Console.WriteLine($"[SERVER] Monster {monster.MonsterName} killed by player {playerId}");

            await _questManager.HandleMonsterKillAsync(session.CharacterID, monster.MonsterID);

            // --- LOGIC RƠI VẬT PHẨM MỚI ---
            List<GroundItem> droppedItems = ProcessItemDrop(monster, playerId);
            if (droppedItems.Any())
            {
                // Thêm vật phẩm vào cache của map
                lock (_lock)
                {
                    if (!_groundItemsByMap.ContainsKey(mapId))
                    {
                        _groundItemsByMap[mapId] = new List<GroundItem>();
                    }
                    _groundItemsByMap[mapId].AddRange(droppedItems);
                }

                // Gửi thông báo cho tất cả người chơi trong map về các vật phẩm vừa rơi ra
                await BroadcastGroundItemsSpawned(mapId, droppedItems);
            }

            // Broadcast cái chết của quái vật (không cần gửi dropItems trong message này nữa)
            await BroadcastMonsterDeath(mapId, monster, playerId, facingRight);
        }
    }

    private List<GroundItem> ProcessItemDrop(MonsterRuntime monster, int killerPlayerId)
    {
        var droppedGroundItems = new List<GroundItem>();

        // Dữ liệu drop của quái có thể là "101:50,102:30" (ItemID:TỷLệPhầnTrăm)
        if (string.IsNullOrEmpty(monster.DropItem))
        {
            return droppedGroundItems;
        }

        var dropEntries = monster.DropItem.Split(','); // Tách các cặp "ItemID:Rate"

        foreach (var entry in dropEntries)
        {
            var parts = entry.Split(':');
            if (parts.Length != 2) continue;

            if (int.TryParse(parts[0], out int itemId) && int.TryParse(parts[1], out int dropRate))
            {
                // Tung xúc xắc, nếu kết quả nhỏ hơn tỷ lệ thì rớt đồ
                if (Random.Shared.Next(100) < dropRate)
                {
                    var groundItem = new GroundItem
                    {
                        ItemID = itemId,
                        X = monster.X, // Rơi ra tại vị trí quái chết
                        Y = monster.Y,
                        OwnerCharacterID = killerPlayerId, // Gán quyền sở hữu
                        ExpiryTime = DateTime.UtcNow.AddSeconds(20)
                    };
                    droppedGroundItems.Add(groundItem);

                    Console.WriteLine($"[Drop] Monster {monster.MonsterName} dropped item {itemId} for player {killerPlayerId}.");
                }
            }
        }

        return droppedGroundItems;
    }

    private async Task BroadcastGroundItemsSpawned(int mapId, List<GroundItem> items)
    {
        var itemDtos = items.Select(item => new
        {
            uniqueId = item.UniqueId,
            itemId = item.ItemID,
            x = item.X,
            y = item.Y,
            ownerId = item.OwnerCharacterID
        }).ToList();

        var message = new BaseMessage
        {
            Action = GroundItemActions.ITEM_SPAWNED,
            Data = JObject.FromObject(new { items = itemDtos })
        };

        await BroadcastToMap(mapId, message);
    }
    // Thêm phương thức mới này vào trong class MonsterManager

    /// <summary>
    /// Xử lý logic tick cho các vật phẩm trên mặt đất, như kiểm tra hết hạn.
    /// </summary>
    private async Task ProcessGroundItemsTick(int mapId)
    {
        List<GroundItem> expiredItems = null;

        lock (_lock)
        {
            // Kiểm tra xem map này có item không
            if (_groundItemsByMap.TryGetValue(mapId, out var itemsOnMap) && itemsOnMap.Any())
            {
                // Lọc ra các item đã hết hạn
                expiredItems = itemsOnMap.Where(item => DateTime.UtcNow >= item.ExpiryTime).ToList();

                // Nếu có item hết hạn, xóa chúng khỏi danh sách chính
                if (expiredItems.Any())
                {
                    itemsOnMap.RemoveAll(item => DateTime.UtcNow >= item.ExpiryTime);
                }
            }
        }

        // Nếu tìm thấy item hết hạn, broadcast thông tin cho client
        if (expiredItems != null && expiredItems.Any())
        {
            Console.WriteLine($"[Despawn] Found {expiredItems.Count} expired items on map {mapId}.");

            // Lấy danh sách các UniqueId để gửi đi
            var expiredItemIds = expiredItems.Select(item => item.UniqueId).ToList();

            // Gửi gói tin
            await BroadcastItemsDespawned(mapId, expiredItemIds);
        }
    }
    // Thêm phương thức mới này vào trong class MonsterManager

    /// <summary>
    /// Gửi thông báo cho client rằng một hoặc nhiều vật phẩm đã biến mất (hết hạn).
    /// </summary>
    private async Task BroadcastItemsDespawned(int mapId, List<Guid> itemUniqueIds)
    {
        // Tạo gói tin với action và dữ liệu
        var message = new BaseMessage
        {
            Action = GroundItemActions.ITEM_DESPAWN, // Sử dụng action đã định nghĩa
            Data = new JObject
            {
                // Gửi một mảng các uniqueId
                ["uniqueIds"] = JArray.FromObject(itemUniqueIds)
            }
        };

        // Gửi đến tất cả người chơi trong map
        await BroadcastToMap(mapId, message);
    }


    private int CalculateExpGain(int monsterLevel)
    {
        return monsterLevel * Random.Shared.Next(5, 15);
    }

  

    private Session? FindNearestPlayerInRange(MonsterRuntime monster, List<Session> playersInMap)
    {
        Session? nearestPlayer = null;
        float minDistance = float.MaxValue;

        foreach (var session in playersInMap)
        {
            if (session.Character.CurrentHealth <= 0)
                continue;

            float dist = GetDistance(monster.X, monster.Y, session.Character.X, session.Character.Y);

            if (dist <= monster.AttackRange && dist < minDistance)
            {
                nearestPlayer = session;
                minDistance = dist;
            }
        }

        return nearestPlayer;
    }

    private float GetDistance(float x1, float y1, float x2, float y2)
    {
        return (float)Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
    }

    /// <summary>
    /// Xoá cache map không còn active
    /// </summary>
    public void CleanupInactiveMaps()
    {
        lock (_lock)
        {
            var activeMapIds = new HashSet<int>(_sessionManager.GetActiveMapIds());
            var mapsToRemove = _cacheMonstersByMap.Keys.Where(mapId => !activeMapIds.Contains(mapId)).ToList();

            foreach (var mapId in mapsToRemove)
            {
                _cacheMonstersByMap.Remove(mapId);
                _mapInitialized.Remove(mapId);
                Console.WriteLine($"[SERVER] Cleaned up monsters for inactive map {mapId}");
            }
        }
    }

    #region Broadcast Methods (Individual - Kept for ProcessPlayerAttackMonster)

    private async Task BroadcastMonsterTakeDamage(int mapId, MonsterRuntime monster, int damage, int playerId)
    {
        var message = new BaseMessage
        {
            Action = MonsterActions.MONSTER_TAKE_DAMAGE,
            Data = new JObject
            {
                ["spawnId"] = monster.SpawnID,
                ["damage"] = damage,
                ["attackerPlayerId"] = playerId,
                ["currentHp"] = monster.CurrentHP,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }
        };

        await BroadcastToMap(mapId, message);
    }

    public async Task ProcessPlayerPickupItem(Session playerSession, Guid itemUniqueId)
    {
        GroundItem? itemToPickup = null;
        int mapId = playerSession.MapID;

        lock (_lock)
        {
            if (_groundItemsByMap.TryGetValue(mapId, out var itemsOnMap))
            {
                itemToPickup = itemsOnMap.FirstOrDefault(i => i.UniqueId == itemUniqueId);
            }
        }

        if (itemToPickup == null)
        {
            // Item không tồn tại hoặc đã được nhặt
            return;
        }

        // KIỂM TRA QUYỀN SỞ HỮU
        if (itemToPickup.OwnerCharacterID != playerSession.CharacterID)
        {
            await _sessionManager.SendToastNotification(playerSession.ClientId, "Bạn không thể nhặt vật phẩm của người khác.", "error");
            return;
        }

        // KIỂM TRA KHOẢNG CÁCH (chống hack)
        float distance = GetDistance(playerSession.Character.X, playerSession.Character.Y, itemToPickup.X, itemToPickup.Y);
        if (distance > 2.0f) // Giả sử tầm nhặt là 3 đơn vị
        {
            await _sessionManager.SendToastNotification(playerSession.ClientId, "Bạn ở quá xa để nhặt.", "error");
            return;
        }

        // Thử thêm vật phẩm vào túi đồ
        bool added = await _sessionManager.AddItemToInventory(playerSession, itemToPickup.ItemID, 1);

        if (added)
        {
            // Nếu thêm thành công, xóa item khỏi mặt đất
            lock (_lock)
            {
                _groundItemsByMap[mapId].Remove(itemToPickup);
            }

            // Broadcast cho mọi người rằng item đã bị nhặt
            await BroadcastItemPickedUp(mapId, itemUniqueId, playerSession.CharacterID);
        }
        // Nếu không thêm được (túi đầy), item vẫn nằm trên mặt đất.
        // Hàm AddItemToInventory đã tự gửi thông báo "Túi đồ đầy".
    }

    private async Task BroadcastItemPickedUp(int mapId, Guid itemUniqueId, int pickerPlayerId)
    {
        var message = new BaseMessage
        {
            Action = GroundItemActions.ITEM_PICKED_UP,
            Data = JObject.FromObject(new
            {
                uniqueId = itemUniqueId,
                pickerId = pickerPlayerId
            })
        };
        await BroadcastToMap(mapId, message);
    }


    private async Task BroadcastMonsterDeath(int mapId, MonsterRuntime monster, int killerPlayerId, bool facingRight)
    {
        var message = new BaseMessage
        {
            Action = MonsterActions.MONSTER_DEATH,
            Data = new JObject
            {
                ["facing"] = facingRight,
                ["spawnId"] = monster.SpawnID,
                ["killerPlayerId"] = killerPlayerId,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }
        };

        await BroadcastToMap(mapId, message);
    }

    public async Task BroadcastToMap(int mapId, BaseMessage message)
    {
        try
        {
            var clientIds = _sessionManager.GetClientIdsInMap(mapId);
            if (clientIds == null || clientIds.Count == 0) return;

            var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

            var tasks = clientIds
                .Where(clientId => _watsonWsServer.IsClientConnected(clientId))
                .Select(clientId => _watsonWsServer.SendAsync(clientId, payload));

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BroadcastToMap] Error while broadcasting to map {mapId}: {ex.Message}");
        }
    }


    #endregion
}