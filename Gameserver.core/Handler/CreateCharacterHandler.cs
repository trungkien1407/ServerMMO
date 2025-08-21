using Gameserver.core.Models;
using Gameserver.core.Services;
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
using Gameserver.core.Dto;
using Microsoft.Extensions.DependencyInjection;
using Gameserver.core.Manager;
using Gameserver.core.Repo.Interfaces;

namespace Gameserver.core.Handler
{
    public class CreateCharacterHandler : IMessageHandler
    {
        public string Action => "create_char";
        private readonly IServiceProvider _serviceProvider;
        private readonly SessionManager _sessionManager;
        private readonly SkillManager _skillManager;
        public CreateCharacterHandler(IServiceProvider provider, SessionManager sessionManager,SkillManager skillManager)
        {
            _serviceProvider = provider;
            _sessionManager = sessionManager;
            _skillManager = skillManager;
        }
        public async Task Handle(Guid clientId, JObject data,WatsonWsServer server)
        {
            using var scope = _serviceProvider.CreateScope();


            var _characterService = scope.ServiceProvider.GetRequiredService<ICharacterService>();
            var _characterSkill = scope.ServiceProvider.GetRequiredService<ISkillRepo>();
            var session = _sessionManager.GetSessionByClientId(clientId);
            if(session == null)
            {
                var response = new BaseMessage
                {
                    Action = "login_required", // Thông báo cần đăng nhập lại
                    Data = new JObject
                    {
                        ["Success"] = false,
                        ["message"] = "Please log in again."
                    }
                    
                };
                await server.SendAsync(clientId, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
                return;
            }
           var charname = data["name"]?.ToString();
           int Class = data["Class"].Value<int>();
            int Gender = data["gender"].Value<int>(); // Cách tốt nhất với JObject
            var checkname = await _characterService.CheckName(charname);
            if (checkname)
            {
                var response = new BaseMessage
                {
                    Action = "create_char_result",
                    Data = new JObject
                    {
                        ["Success"] = false,
                        ["message"] = "Ten nhan vat da ton tai.",
                      
                    }
                };
                await server.SendAsync(clientId, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
                return;
            }
            var newcharacter = new Characters(charname, session.UserID,Gender,Class);
            var skill = Class == 1 ? 101 : 102;
         
            var createdchar = await _characterService.CreateAsync(newcharacter);
            if (createdchar != null)
            {
                session.Character = createdchar;
                session.CharacterID = createdchar.CharacterID;
                await _characterService.LoadCharacterDataIntoSession(session, Class);
                var characterSKill = new Characterskill
                {
                    CharacterID = createdchar.CharacterID,
                    SkillID = skill,
                    Level = 1,
                };
             await   _characterSkill.LernSkill(characterSKill);
                var response = new BaseMessage
                {
                    Action = "create_char_result",
                    Data = new JObject
                    {
                        ["Success"] = true,
                        ["message"] = "Character created successfully.",
                        ["character"] = JObject.FromObject(createdchar),
                        ["islocal"] = true
                    }
                };
                await _skillManager.LoadSkillsIfNeededAsync(createdchar.CharacterID);
                var skills = _skillManager.GetSkillDTOs(createdchar.CharacterID);
                Console.WriteLine(JObject.FromObject(createdchar));
                

                await server.SendAsync(clientId, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
                await SendSkill(skills, server, clientId);
            }
            else
            {
                var response = new BaseMessage
                {
                    Action = "create_char_result",
                    Data = new JObject
                    {
                        ["Success"] = false,
                        ["message"] = "False to Create."
                    }
                };
                await server.SendAsync(clientId, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
            }



        }

        public async Task SendSkill(List<SkillDto> skillid, WatsonWsServer server, Guid clientID)
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
