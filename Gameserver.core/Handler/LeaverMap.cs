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
    public class LeaverMap:IMessageHandler
    {
        public string Action => "Leavermap";
        private readonly SessionManager _sessionManager;
        public LeaverMap(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public async Task Handle(Guid clientId, JObject data, WatsonWsServer server)
        {
            var mapid = data["mapid"]?.ToObject<int>() ?? 0;
            //var characterid = data["characterid"]?.ToObject<int>() ?? 0;
           // if (mapid == 0 || characterid == 0) return;
            var clientinmap = _sessionManager.GetClientIdsInMap(mapid);
            // var userid = _sessionManager.GetUserIdByClientId(clientId);
            
            var session = _sessionManager.GetSessionByClientId(clientId);
            if (session == null) return;
            var character = session.Character.CharacterID;
            var respone = new BaseMessage()
            {
                Action = "Leavermap",
                Data = new JObject
                {
                    ["characterid"]=character,
                }
            };
            var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(respone));
            Console.WriteLine(character);

            var sendTasks = clientinmap
                .Where(otherClientId => otherClientId != clientId && server.IsClientConnected(otherClientId))
                .Select(otherClientId => server.SendAsync(otherClientId, payload));

            await Task.WhenAll(sendTasks);
        }
    }
}
