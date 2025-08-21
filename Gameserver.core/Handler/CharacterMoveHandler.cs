using Gameserver.core.Dto;
using Gameserver.core.Network;
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
    public class CharacterMoveHandler : IMessageHandler
    {
        public string Action => "Charactermove";
        private readonly SessionManager _sessionManager;
        public CharacterMoveHandler(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
           
        }

        public async Task Handle(Guid clientId, JObject data, WatsonWsServer server)
        {
            var session = _sessionManager.GetSessionByClientId(clientId);
            if (session == null || session.Character == null) return;
            int characterId = session.Character.CharacterID;
            int mapId = session.MapID;
            var clientinmap = _sessionManager.GetClientIdsInMap(mapId);
            var playerinmap = _sessionManager.GetPlayersInMap(mapId);
            // 2. Parse data từ client
            bool isMoving = data["isMoving"]?.ToObject<bool>() ?? false;
            int dir = data["dir"]?.ToObject<int>() ?? 1;
            float x = data["x"]?.ToObject<float>() ?? 1;
            float y = data["y"]?.ToObject<float>() ?? 1;
            session.Character.UpdatePositon(x, y);
            float movespeed = data["movespeed"]?.ToObject<float>() ?? 0f;
            var respone = new BaseMessage
            {
                Action = "CharactermoveUpdate",
                Data = new JObject
                {
                    ["movespeed"] = movespeed,
                    ["characterid"] = characterId,
                    ["isMoving"] = isMoving,
                    ["dir"] = dir, 
                }
            };

            var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(respone));
            var tasks = clientinmap
             .Where(client => server.IsClientConnected(client) && client != clientId)
            .Select(client => server.SendAsync(client, payload));

            await Task.WhenAll(tasks);

        }
       
    }

}
