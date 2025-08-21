using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace Gameserver.core.Handler
{
        public interface IMessageHandler
        {
            string Action { get; } // Ví dụ: "login", "register"
            Task Handle(Guid clientId, JObject data, WatsonWsServer server);
        }

 
}
