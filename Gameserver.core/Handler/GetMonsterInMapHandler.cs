using Gameserver.core.Dto;
using Gameserver.core.Network;
using Gameserver.core.Repo.Interfaces;
using Gameserver.core.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace Gameserver.core.Handler
{
    public class GetMonsterInMapHandler : IMessageHandler
    {
        public string Action => "Getmonster_map";
        private readonly MonsterManager _monsterManager;
       private  IServiceProvider _serviceProvider;

        public GetMonsterInMapHandler(MonsterManager monsterManager,IServiceProvider serviceProvider)
        {
            _monsterManager = monsterManager;
            _serviceProvider = serviceProvider;
        }

        public async Task Handle(Guid clientId, JObject data, WatsonWsServer server)
        {
            using var scope = _serviceProvider.CreateScope();

            var npcRepo = scope.ServiceProvider.GetRequiredService<INPCRepo>();
            if (data == null)
            {
                Console.WriteLine("GetmonsterHandler data is Null");
                return;
            }

            int mapid = data["mapId"].Value<int>();

            // Load monsters cho map - CHỈ init nếu chưa có
            var monsters = await _monsterManager.LoadMonstersForMapAsync(mapid);
            var npcs = await npcRepo.GetNPCByMap(mapid);
            // Tạo response với trạng thái hiện tại của monsters
            var response = monsters.Select(monster => new
            {
                spawnId = monster.SpawnID,
                monsterId = monster.MonsterID,
                MonsterName = monster.MonsterName,
                MonsterImg = monster.MonsterImg,
                x = Math.Round(monster.X, 2), // Làm tròn để consistency
                y = Math.Round(monster.Y, 2),
                currentHp = monster.CurrentHP,
                maxHp = monster.MaxHP,
                isAlive = monster.IsAlive,
                level = monster.Level,
                // Thêm thông tin bổ sung nếu cần
                attackRange = monster.AttackRange,
                
            }).ToList();
            var npcRespone = new BaseMessage
            {
                Action = "NPC",
                Data = JObject.FromObject(new
                {
                    npc = npcs,
                })
            };
            var responseMessage = new BaseMessage
            {
                Action = "MonsterList",
                Data = JObject.FromObject(new
                {
                    mapId = mapid,
                    monsters = response,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                })
            };

            Console.WriteLine($"[GetMonsterInMapHandler] Sending {response.Count} monsters for map {mapid}");
            
            var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(responseMessage));
            await server.SendAsync(clientId, payload);
            await server.SendAsync(clientId, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(npcRespone)));
        }
    }
}