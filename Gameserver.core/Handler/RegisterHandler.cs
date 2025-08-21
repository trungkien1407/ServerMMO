using Gameserver.core.Services.Interfaces;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonWebsocket;
using System.Diagnostics;
using Gameserver.core.Dto;
using Gameserver.core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gameserver.core.Handler
{
    public class RegisterHandler : IMessageHandler
    {
        public string Action => "register";

        private readonly IServiceProvider _serviceProvider;

        public RegisterHandler(IServiceProvider provider)
        {
            _serviceProvider = provider;
        }
        public async Task Handle(Guid clientId, JObject data, WatsonWsServer server)
        {
            using var scope = _serviceProvider.CreateScope();

            var _accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();
           
            var username = data["username"]?.ToString();
            var password = data["password"]?.ToString();
            var email = data["email"]?.ToString();
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(email)) return;
            var result = await _accountService.Register(username, password,email);
            if (result == "Success")
            {
                var response = new BaseMessage
                {
                   Action = "register_result",
                   Data = new JObject
                   {
                       ["success"] = true,
                       ["message"] = "Dang Ky thanh cong"
                   }
                };
                await server.SendAsync(clientId, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
            }
            else
            {

                var response = new BaseMessage
                {
                    Action = "register_result",
                    Data = new JObject
                    {
                        ["success"] = false,
                        ["message"] = result
                    }
                };
                Console.WriteLine(result);
                await server.SendAsync(clientId, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
            }
        }
    }
}
