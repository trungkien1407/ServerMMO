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
    public class CharacterJump : IMessageHandler
    {
        public string Action => "CharacterJump";
        private readonly SessionManager _sessionManager;
        public CharacterJump(SessionManager sessionManager)
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
            float movespeed = data["force"]?.ToObject<float>() ?? 0f;
            var respone = new BaseMessage
            {
                Action = "CharacterJump",
                Data = new JObject
                {
                    ["force"] = movespeed,
                    ["characterid"] = characterId,
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
