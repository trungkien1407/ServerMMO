using Gameserver.core.Services;
using Gameserver.core.Network;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonWebsocket;
using Gameserver.core.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Gameserver.core.Dto;

namespace Gameserver.core.Handler
{
    public class LogoutHandler :IMessageHandler
    {
        public string Action => "logout";
        private readonly SessionManager _sessionManager;
      
    private readonly IServiceProvider _serviceProvider;

        public LogoutHandler(SessionManager sessionManager, IServiceProvider serviceProvider)
        {
            _sessionManager = sessionManager;
            _serviceProvider =  serviceProvider;
        }

        public async Task Handle(Guid clientId, JObject data, WatsonWsServer server)
        {

            using var scope = _serviceProvider.CreateScope();

           
            var _characterService = scope.ServiceProvider.GetRequiredService<ICharacterService>();
            try
            {
                // Tìm userID tương ứng với clientId trong session
                var session = _sessionManager.GetSessionByClientId(clientId);
                if (session == null)
                {
                    await SendAsync(server, clientId, new
                    {
                        action = "logout_result",
                        success = false,
                        message = "Session not found"
                    });
                    return;
                }

                int userId = session.UserID;

                 await  _sessionManager.SaveData(clientId);

                // Xoá session
                _sessionManager.RemoveSessionByClientId(clientId);

                var result = new BaseMessage
                {
                    Action = "logout_result",
                    Data = new JObject
                    {
                        ["success"] = true,
                        ["message"] = "Logout complete"
                    }
                };
                // Gửi phản hồi
                await SendAsync(server, clientId, result);
               
            }
            catch (Exception ex)
            {
                // Ghi log nếu cần
                Console.WriteLine($"Error during logout: {ex.Message}");
                var result = new BaseMessage
                {
                    Action = "logout_result",
                    Data = new JObject
                    {
                        ["success"] = false,
                        ["message"] = "error server"
                    }
                };
                await SendAsync(server, clientId, result);

            }
        }

        private async Task SendAsync(WatsonWsServer server, Guid clientId, object data)
        {
            var json = JsonConvert.SerializeObject(data);
            await server.SendAsync(clientId, Encoding.UTF8.GetBytes(json));
        }
    }

}
