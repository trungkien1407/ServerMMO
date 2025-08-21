using Gameserver.core.Dto;
using Gameserver.core.Models;
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
    public class PlayerRespawnHandle : IMessageHandler
    {
        public string Action => "Respawn_player";
        private readonly SessionManager _sessionManager;

        public PlayerRespawnHandle(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public async Task Handle(Guid clientId, JObject data, WatsonWsServer server)
        {
            var session = _sessionManager.GetSessionByClientId(clientId);
            if (session?.Character == null)
            {
                Console.WriteLine($"PlayerRespawn: Session not found for client {clientId}");
                return;
            }

            int characterId = data["characterId"]?.Value<int>() ?? session.Character.CharacterID;
            int originalMapId = session.Character.MapID;
            Console.WriteLine($"Player {characterId} requesting respawn from map {originalMapId}");

            // **LUÔN RESPAWN VỀ MAP 1**
            const int RESPAWN_MAP_ID = 1;

            // Nếu player đang ở map khác, gửi leave message cho map cũ
            if (originalMapId != RESPAWN_MAP_ID)
            {
                await SendLeaveMapMessage(server, session, originalMapId);
                _sessionManager.UpdatePlayerMap(session.UserID, RESPAWN_MAP_ID);
                session.Character.MapID = RESPAWN_MAP_ID;
            }

            // Set spawn position cho map 1
            session.Character.X = -9;
            session.Character.Y = -5;

            // Reset health và mana về max
            session.Character.CurrentHealth = session.Character.Health;
            session.Character.CurrentMana = session.Character.Mana;

           

            // Tạo response message
            var message = new BaseMessage
            {
                Action = "player_respawn",
                Data = new JObject
                {
                    ["character"] = JObject.FromObject(session.Character)
                }
            };

            var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

            // Gửi cho tất cả clients trong map 1 (bao gồm cả player vừa respawn)
            var clientsInMap1 = _sessionManager.GetClientIdsInMap(RESPAWN_MAP_ID);

            Console.WriteLine($"Character {characterId} respawned at map {RESPAWN_MAP_ID} ({session.Character.X}, {session.Character.Y})");
            Console.WriteLine($"Sending respawn notification to {clientsInMap1.Count()} clients in map {RESPAWN_MAP_ID}");

            // Gửi cho tất cả clients trong map 1
            var sendTasks = clientsInMap1
                .Where(otherClientId => server.IsClientConnected(otherClientId))
                .Select(otherClientId => server.SendAsync(otherClientId, payload));

            await Task.WhenAll(sendTasks);
        }

        // Optional: Gửi leave message nếu player chuyển map
        private async Task SendLeaveMapMessage(WatsonWsServer server, Session session, int oldMapId)
        {
            var leaveMessage = new BaseMessage
            {
                Action = "Leavermap",
                Data = new JObject
                {
                    ["characterid"] = session.Character.CharacterID,
                    ["mapid"] = oldMapId
                }
            };

            var leavePayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(leaveMessage));
            var oldMapClients = _sessionManager.GetClientIdsInMap(oldMapId);

            Console.WriteLine($"Sending leave message to {oldMapClients.Count()} clients in old map {oldMapId}");

            var leaveTasks = oldMapClients
                .Where(clientId => server.IsClientConnected(clientId))
                .Select(clientId => server.SendAsync(clientId, leavePayload));

            await Task.WhenAll(leaveTasks);
        }

        // Optional: Gửi leave message nếu player chuyển map
      
    }
}


