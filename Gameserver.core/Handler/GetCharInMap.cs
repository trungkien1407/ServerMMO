using Gameserver.core.Services;
using Gameserver.core.Dto;
using Gameserver.core.Network;
using Gameserver.core.Services.Interfaces;
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
    public class GetCharInMap : IMessageHandler
    {
        public string Action => "Characters_in_map";

       
        private readonly SessionManager _sessionManager;

        public GetCharInMap(ICharacterService characterService, SessionManager sessionManager)
        {
            
            _sessionManager = sessionManager;
        }

        public async Task Handle(Guid clientId, JObject data, WatsonWsServer server)
        {
            // Lấy session
            var session = _sessionManager.GetSessionByClientId(clientId);
            if (session == null )
            {
                Console.WriteLine($"[Characters_in_map] Invalid session or character not found for client: {clientId}");
                return;
            }

            int mapId = data["mapid"]?.ToObject<int>() ?? -1;

            if (mapId <= 0)
            {
                Console.WriteLine($"[Characters_in_map] Invalid mapId: {mapId}");
                return;
            }

            // Lấy danh sách các nhân vật đang ở trong map
            var charactersInMap = _sessionManager.GetSessions()
                .Where(s => s.Value.Character != null && s.Value.Character.MapID == mapId)
                .Select(s => new
                {
                   s.Value.Character.CharacterID,
                   s.Value.Character.Name,
                   s.Value.Character.Gender,
                   s.Value.Character.HeadID,
                   s.Value.Character.BodyID,
                   s.Value.Character.WeaponID,
                   s.Value.Character.PantID,
                   s.Value.Character.Level,
                   s.Value.Character.Class,
                   s.Value.Character.Exp,
                   s.Value.Character.Health,
                   s.Value.Character.Mana,
                   s.Value.Character.CurrentHealth,
                   s.Value.Character.CurrentMana,
                   s.Value.Character.Strength,
                   s.Value.Character.Intelligence,
                   s.Value.Character.Dexterity,
                   s.Value.Character.X,
                   s.Value.Character.Y,
                   s.Value.Character.MapID,
                   s.Value.Character.Gold,


                })
                .ToList();

            var charactersArray = JArray.FromObject(charactersInMap);
            var dataObject = new JObject();
            dataObject["characters"] = charactersArray;

            var response = new BaseMessage
            {
                Action = "Characters_in_map",
                Data = dataObject
            };
            Console.WriteLine(dataObject);

            await server.SendAsync(clientId, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
        }
    }
}
