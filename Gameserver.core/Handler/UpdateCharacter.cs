using Gameserver.core.Dto;
using Gameserver.core.Network;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace Gameserver.core.Handler
{
    public class UpdateCharacter : IMessageHandler
    {
        public string Action => "update_data";
        private readonly SessionManager _sessionManager;
        public UpdateCharacter(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public async Task Handle(Guid clientId, JObject data, WatsonWsServer server)
        {
            var session = _sessionManager.GetSessionByClientId(clientId);
            if (session == null || session.Character == null) return;
            // 2. Parse data từ client
            float x = data["x"]?.ToObject<float>() ?? 1;
            float y = data["y"]?.ToObject<float>() ?? 1;
             session.Character.UpdatePositon(x, y);
            await Task.CompletedTask;
        }
    }
}
