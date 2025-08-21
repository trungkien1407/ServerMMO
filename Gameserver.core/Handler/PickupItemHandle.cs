using Gameserver.core.Manager;
using Gameserver.core.Network;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace Gameserver.core.Handler
{
    public class PickupItemHandle : IMessageHandler
    {
        public string Action => "pickup_item";

        private readonly SessionManager _sessionManager;
        private readonly MonsterManager _monsterManager; // Cần inject MonsterManager

        public PickupItemHandle(SessionManager sessionManager, MonsterManager monsterManager)
        {
            _sessionManager = sessionManager;
            _monsterManager = monsterManager;
        }

        public async Task Handle(Guid clientId, JObject data, WatsonWsServer server)
        {
            var session = _sessionManager.GetSessionByClientId(clientId);
            if (session == null || session.Character == null) return;

            if (!Guid.TryParse(data["uniqueId"]?.Value<string>(), out Guid uniqueId))
            {
                return; // Dữ liệu không hợp lệ
            }

            // Gọi phương thức xử lý trong MonsterManager
            await _monsterManager.ProcessPlayerPickupItem(session, uniqueId);
        }
    }
}