using Gameserver.core.Dto;
using Gameserver.core.Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace Gameserver.core.Handler
{
    public class CharacterJoinMap: IMessageHandler
    {
        public string Action => "Joinmap";
        private readonly SessionManager _sessionManager;  // Thêm SessionManager vào constructor
        public CharacterJoinMap(SessionManager sessionManager )
        {
            _sessionManager = sessionManager;
        }

        public async Task Handle(Guid clientId,JObject data,WatsonWsServer server)
        {
            var mapid = data["mapid"]?.ToObject<int>() ?? 0;
            var x = data["spawnX"]?.ToObject<float>() ?? 0;
            var y = data["spawnY"]?.ToObject<float>() ?? 0;
            //  var characterid = data["characterid"]?.ToObject<int>() ?? 0;
            // if (mapid == 0|| characterid ==0) return;
            var session = _sessionManager.GetSessionByClientId(clientId);
            _sessionManager.UpdatePlayerMap(session.UserID, mapid);
            var clientinmap = _sessionManager.GetClientIdsInMap(mapid);
           // var userid = _sessionManager.GetUserIdByClientId(clientId);
            if (session == null) return;
            var character = session.Character;
            if (character == null) return;
            character.X = x;
            character.Y = y;
            var respone = new BaseMessage()
            {
                Action = "Joinmap",
                Data = JObject.FromObject(new
                {
                    character,
                })

            };
            var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(respone));
            Console.WriteLine(JsonConvert.SerializeObject(character));
            //Console.WriteLine($"charracter {character.CharacterID} spawn at map{character.MapID}");
            var sendTasks = clientinmap
                .Where(otherClientId => otherClientId != clientId && server.IsClientConnected(otherClientId))
                .Select(otherClientId => server.SendAsync(otherClientId, payload));

            await Task.WhenAll(sendTasks);
        }

      
    }
}
