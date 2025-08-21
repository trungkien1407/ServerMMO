using Gameserver.core.Dto;
using Gameserver.core.Manager;
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
    public class TalkQuestHandler : IMessageHandler
    {
        public string Action => "talk_quest";
        private readonly QuestManager _questManager;
        private readonly SessionManager _sessionManager;

        public TalkQuestHandler(QuestManager questManager, SessionManager sessionManager)
        {
            _questManager = questManager;
            _sessionManager = sessionManager;
        }

        public async Task Handle(Guid clientId, JObject data, WatsonWsServer server)
        {
            var session = _sessionManager.GetSessionByClientId(clientId);
            if (session == null || session.Character == null)
                return;

            string type = data["type"]?.ToString();
            int characterId = session.CharacterID;

            // 👉 Nếu type là get_active thì trả về danh sách quest đang nhận
            if (type == "get_active")
            {
                var activeQuests = await _questManager.GetActiveQuestsAsync(characterId);
                var questsWithProgress = new List<object>();

                foreach (var quest in activeQuests)
                {
                    var (progress, objectives) = await _questManager.GetQuestProgressAsync(characterId, quest.QuestID);

                    // Kiểm tra null và tạo objectiveList
                    List<object> objectiveList = new List<object>();
                    if (objectives != null)
                    {
                        objectiveList = objectives.Select(obj => new
                        {
                            Type = obj.Original.Type,
                            TargetID = obj.Original.TargetID,
                            Current = obj.TempAmount,
                            Required = obj.Original.RequiredAmount,
                            IsComplete = obj.IsComplete
                        }).Cast<object>().ToList();
                    }

                    questsWithProgress.Add(new
                    {
                        quest.QuestID,
                        quest.Name,
                        quest.Description,
                        Status = progress?.Status ?? "Unknown",
                        Objectives = objectiveList
                    });
                }

                var response = new BaseMessage
                {
                    Action = "active_quests",
                    Data = JObject.FromObject(new
                    {
                        quests = questsWithProgress
                    })
                };

                await server.SendAsync(clientId, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
                return;
            }

            // 👉 NEW: Xử lý client confirm complete quest
            if (type == "complete_quest")
            {
                int questId = data["questId"]?.Value<int>() ?? 0;

                // Kiểm tra quest có đang pending reward không
                if (_questManager.IsQuestPendingReward(characterId, questId))
                {
                    await _questManager.CompleteAndChainNextAsync(characterId, questId);

                    var response = new BaseMessage
                    {
                        Action = "quest_rewarded",
                        Data = JObject.FromObject(new
                        {
                            questId = questId,
                            message = "Bạn đã hoàn thành nhiệm vụ và nhận phần thưởng."
                        })
                    };
                    await server.SendAsync(clientId, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
                }
                return;
            }

            // ----- Logic cũ cho talk với NPC -----

            int npcId = data["npcId"]?.Value<int>() ?? 0;

            var activeQuestsWithNpc = await _questManager.GetActiveQuestsAsync(characterId);
            var currentQuest = activeQuestsWithNpc.FirstOrDefault(q => q.StartNPCID == npcId || q.EndNPCID == npcId);

            if (currentQuest != null)
            {
                var (progress, objectives) = await _questManager.GetQuestProgressAsync(characterId, currentQuest.QuestID);

                //  Kiểm tra quest đang pending reward
                if (progress.Status == "PendingReward")
                {
                    // Quest sẵn sàng để complete, chờ client confirm
                    var response = new BaseMessage
                    {
                        Action = "quest_can_complete",
                        Data = JObject.FromObject(new
                        {
                            quest = currentQuest,
                            questId = currentQuest.QuestID,
                            message = "Xuất sắc! Bạn đã hoàn thành nhiệm vụ. Nhấn để nhận thưởng.",
                            canComplete = true
                        })
                    };
                    await server.SendAsync(clientId, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
                }
                else if (objectives != null && objectives.All(o => o.IsComplete))
                {
                    // Trường hợp backup - không nên xảy ra nếu logic đúng
                    await _questManager.SetQuestPendingRewardAsync(characterId, currentQuest.QuestID);

                    var response = new BaseMessage
                    {
                        Action = "quest_can_complete",
                        Data = JObject.FromObject(new
                        {
                            quest = currentQuest,
                            questId = currentQuest.QuestID,
                            message = "Xuất sắc! Bạn đã hoàn thành nhiệm vụ. Nhấn để nhận thưởng.",
                            canComplete = true
                        })
                    };
                    await server.SendAsync(clientId, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
                }
                else
                {
                    // Quest vẫn đang in progress
                    List<object> questObjectives = new List<object>();
                    if (objectives != null)
                    {
                        questObjectives = objectives.Select(obj => new
                        {
                            Type = obj.Original.Type,
                            TargetID = obj.Original.TargetID,
                            Current = obj.TempAmount,
                            Required = obj.Original.RequiredAmount,
                            IsComplete = obj.IsComplete
                        }).Cast<object>().ToList();
                    }

                    var response = new BaseMessage
                    {
                        Action = "quest_in_progress",
                        Data = JObject.FromObject(new
                        {
                            quest = currentQuest,
                            objectives = questObjectives,
                            message = "Bạn chưa hoàn thành nhiệm vụ."
                        })
                    };
                    await server.SendAsync(clientId, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
                }
            }
            else
            {
                // Không có quest active với NPC này, tìm quest mới
                var availableQuests = await _questManager.GetAvailableQuestsAsync(characterId, session.Character.Level);
                var npcQuests = availableQuests.Where(q => q.StartNPCID == npcId).ToList();

                if (npcQuests.Count > 0)
                {
                    var questToStart = npcQuests.OrderBy(q => q.QuestID).First();
                    await _questManager.StartQuestAsync(characterId, questToStart.QuestID);

                    var response = new BaseMessage
                    {
                        Action = "npc_quest_start",
                        Data = JObject.FromObject(new
                        {
                            quest = questToStart
                        })
                    };
                    await server.SendAsync(clientId, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
                }
                else
                {
                    var response = new BaseMessage
                    {
                        Action = "npc_no_quest",
                        Data = JObject.FromObject(new
                        {
                            message = "Hiện tại ta không có việc gì cho con cả."
                        })
                    };
                    await server.SendAsync(clientId, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
                }
            }
        }
    }
}