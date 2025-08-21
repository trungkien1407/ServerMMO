using Gameserver.core.Services;
using Gameserver.core.Dto;
using Gameserver.core.Handler;
using Gameserver.core.Network;
using Gameserver.core.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;
using WatsonWebsocket;
using Gameserver.core.Models;
using Gameserver.core.Manager;

namespace Gameserver.core.Handler
{
    public class LoginHandler : IMessageHandler
    {
        public string Action => "login";

        private readonly IServiceProvider _serviceProvider;
        private readonly SessionManager _sessionManager;
        private readonly SkillManager _skillManager;


        public LoginHandler(IServiceProvider serviceProvider, SessionManager sessionManager,SkillManager skillManager)
        {
            _serviceProvider = serviceProvider;
            _sessionManager = sessionManager;
            _skillManager = skillManager;
        }

        public async Task Handle(Guid clientId, JObject data, WatsonWsServer server)
        {
            using var scope = _serviceProvider.CreateScope();

            var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();
            var characterService = scope.ServiceProvider.GetRequiredService<ICharacterService>();

            var username = data["username"]?.ToString();
            var password = data["password"]?.ToString();
            if(string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Console.WriteLine("data is null");
                return;
            }
            var result = await accountService.Login(username, password);
            if (result.Success)
            {
                if (!_sessionManager.CanLogin(result.UserID))
                {
                    var response = new BaseMessage
                    {
                        Action = "login_result",
                        Data = new JObject
                        {
                            ["success"] = false,
                            ["message"] = "Vui Lòng đăng nhập sau 5s"
                        }
                    };
                    await server.SendAsync(clientId, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
                    return;
                }
                var session = _sessionManager.CreateOrReplaceSession(result.UserID, clientId);


                var hasChar = await characterService.GetByUserIdAsyncClient(result.UserID);
              

                if (hasChar != null)
                {
                    var response = new BaseMessage
                    {
                        Action = "login_result",
                        Data = new JObject
                        {
                            ["success"] = true,
                            ["message"] = result.Message,
                            ["create"] = false,
                            ["character"] = JObject.FromObject(hasChar),
                            ["islocal"] = true
                        }
                    };
                    session.CharacterID = hasChar.CharacterID;
                    session.Character = hasChar;
                    await characterService.LoadCharacterDataIntoSession(session,hasChar.Class);
                    _sessionManager.UpdatePlayerMap(result.UserID, hasChar.MapID);
                    await _skillManager.LoadSkillsIfNeededAsync(hasChar.CharacterID);
                    var skillID = _skillManager.GetSkillDTOs(hasChar.CharacterID);
                   
                 
                    await server.SendAsync(clientId, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
                  await  SendSkill(skillID, server, clientId);
                }
                else
                {
                    var response = new BaseMessage
                    {
                        Action = "login_result",
                        Data = new JObject
                        {
                            ["success"] = true,
                            ["message"] = result.Message,
                            ["create"] = true
                        }
                    };
                    await server.SendAsync(clientId, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
                }
            }
            else
            {
                var response = new BaseMessage
                {
                    Action = "login_result",
                    Data = new JObject
                    {
                        ["success"] = false,
                        ["message"] = result.Message
                    }
                };
                await server.SendAsync(clientId, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
            }
        }

        public async Task SendSkill(List<SkillDto> skillid,WatsonWsServer server,Guid clientID)
        {
            var message = new BaseMessage
            {
                Action = "character_skill",
                Data = JObject.FromObject(new
                {
                    skills = skillid,
                })
               
            };
            await server.SendAsync(clientID, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)));
        }
    }
}
