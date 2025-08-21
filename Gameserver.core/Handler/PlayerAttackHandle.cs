using Gameserver.core.Dto;
using Gameserver.core.Manager;
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
    public class PlayerAttackHandle : IMessageHandler
    {
        public string Action => "Player_attack";
        private readonly MonsterManager _monsterManager;
        private readonly SessionManager _sessionManager;
        private readonly SkillManager _skillManager;

        public PlayerAttackHandle(MonsterManager monsterManager, SessionManager sessionManager, SkillManager skillManager)
        {
            _monsterManager = monsterManager;
            _sessionManager = sessionManager;
            _skillManager = skillManager;
        }

        public async Task Handle(Guid clientId, JObject data, WatsonWsServer server)
        {
            try
            {
                Console.WriteLine($"[DEBUG] PlayerAttackHandle received data: {data}");

                // Validate input data
                var spawnId = data["targetID"]?.ToObject<int>() ?? 0;
                var skillId = data["skillID"]?.ToObject<int>() ?? 0;
                var facing = data["facingRight"]?.ToObject<bool>() ?? false;

                if (spawnId <= 0)
                {
                    Console.WriteLine($"[ERROR] Invalid targetID: {spawnId}");
                    return;
                }

                // Get session
                var session = _sessionManager.GetSessionByClientId(clientId);
                if (session == null)
                {
                    Console.WriteLine($"[ERROR] Session not found for clientId: {clientId}");
                    return;
                }

                var character = session.Character;
                if (character == null)
                {
                    Console.WriteLine($"[ERROR] Character not found for session");
                    return;
                }

                // Check if character is dead
                if (character.IsDead())
                {
                    Console.WriteLine($"[ERROR] Character {character.CharacterID} is dead, cannot attack");
                    return;
                }

                var characterId = character.CharacterID;
                var mapId = session.MapID;

                // Calculate damage (fix typo: damge -> damage)
                var skillDamage = CalculateDamage(characterId, skillId);
                var totalDamage = character.Strength + skillDamage;
                var clientinmap = _sessionManager.GetClientIdsInMap(mapId);
                Console.WriteLine($"[DEBUG] Player {characterId} attacking monster {spawnId} with {totalDamage} damage (Strength: {character.Strength}, Skill: {skillDamage})");

                // Process attack
                await _monsterManager.ProcessPlayerAttackMonster(mapId, characterId, spawnId, totalDamage, session,facing);
                var message = new BaseMessage 
                { Action = "Player_attack",
                Data = new JObject
                {
                    ["characterID"]= characterId,
                    ["Skill"] = skillId,
                    ["Target"] = spawnId,
                    ["facingRight"]= facing,
                }
                };
                Console.WriteLine($"Character attack {characterId}");
                var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
                Console.WriteLine(JsonConvert.SerializeObject(character));
                //Console.WriteLine($"charracter {character.CharacterID} spawn at map{character.MapID}");
                var sendTasks = clientinmap
                    .Where(otherClientId => otherClientId != clientId && server.IsClientConnected(otherClientId))
                    .Select(otherClientId => server.SendAsync(otherClientId, payload));
                await Task.WhenAll(sendTasks);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] PlayerAttackHandle exception: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            }
        }
        
        // Fix method name typo: CaculateDamage -> CalculateDamage
        public int CalculateDamage(int characterId, int skillId)
        {
            try
            {
                if (skillId <= 0)
                {
                    Console.WriteLine($"[DEBUG] No skill used (skillId: {skillId}), returning 0 skill damage");
                    return 0;
                }

                var skill = _skillManager.GetSkill(characterId, skillId);
                if (skill == null)
                {
                    Console.WriteLine($"[WARNING] Skill {skillId} not found for character {characterId}");
                    return 0;
                }

                var damage = skill.CalculateDamage();
                Console.WriteLine($"[DEBUG] Skill {skillId} damage: {damage}");
                return damage;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] CalculateDamage exception: {ex.Message}");
                return 0;
            }
        }
    }
}