using Gameserver.core.Dto;
using Gameserver.core.Models;
using Gameserver.core.Network;
using Gameserver.core.Repo.Interfaces;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace Gameserver.core.Manager
{
    public class QuestManager
    {
        private readonly QuestCacheService _cacheService;
        private readonly WatsonWsServer _server;
        private readonly IQuestRepository _repository;
        private readonly SessionManager _sessionManager;
        private readonly Dictionary<int, CharacterQuestData> _characterQuests = new();

        public QuestManager(QuestCacheService cacheService, IQuestRepository repository, WatsonWsServer server, SessionManager sessionManager)
        {
            _cacheService = cacheService;
            _repository = repository;
            _server = server;
            _sessionManager = sessionManager;
        }
        public enum QuestStatus
        {
            InProgress,
            PendingReward,
            Completed
        }
        public async Task LoadCharacterQuestsAsync(int characterId)
        {
            if (_characterQuests.ContainsKey(characterId)) return;

            var characterData = new CharacterQuestData { CharacterID = characterId };
            var progresses = await _repository.GetQuestProgressByCharacterAsync(characterId);

            foreach (var progress in progresses)
            {
                if (progress.Status == "Completed")
                {
                    characterData.CompletedQuests.Add(progress.QuestID);
                }
                else if (progress.Status == "InProgress" || progress.Status == "PendingReward")
                {
                    characterData.ActiveQuests[progress.QuestID] = progress;

                    var objectives = await _repository.GetQuestObjectivesAsync(progress.QuestProgressID);
                    characterData.QuestObjectives[progress.QuestProgressID] = objectives.Select(obj => new QuestObjectiveState
                    {
                        Original = obj,
                        TempAmount = obj.CurrentAmount
                    }).ToList();
                }
            }

            _characterQuests[characterId] = characterData;
        }

        public async Task<bool> StartQuestAsync(int characterId, int questId)
        {
            var quest = _cacheService.GetQuest(questId);
            if (quest == null) return false;

            await LoadCharacterQuestsAsync(characterId);
            var characterData = _characterQuests[characterId];

            if (characterData.CompletedQuests.Contains(questId)) return false;
            if (characterData.ActiveQuests.ContainsKey(questId)) return false;

            var progress = new Questprogress
            {
                CharacterID = characterId,
                QuestID = questId,
                Status = "InProgress",
                StartTime = DateTime.Now,
            };

            // Save progress và lấy ID được tạo
            await _repository.SaveQuestProgressAsync(progress);

            // Bây giờ progress.QuestProgressID đã có giá trị
            characterData.ActiveQuests[questId] = progress;

            // LẤY TEMPLATE OBJECTIVES TỪ QUEST DEFINITION (cần thêm method này)
            var questTemplateObjectives = await _repository.GetQuestTemplateObjectivesAsync(questId);

            // TẠO OBJECTIVE RECORDS CHO CHARACTER NÀY
            var objectiveStates = new List<QuestObjectiveState>();

            foreach (var template in questTemplateObjectives)
            {
                var newObjective = new Questprogressobjective
                {
                    QuestProgressID = progress.QuestProgressID,
                    Type = template.Type,
                    TargetID = template.TargetID,
                    CurrentAmount = 0, // Bắt đầu từ 0
                    RequiredAmount = template.RequiredAmount
                };

                // Save vào database
                await _repository.SaveQuestObjectiveAsync(newObjective);

                // Add vào memory
                objectiveStates.Add(new QuestObjectiveState
                {
                    Original = newObjective,
                    TempAmount = 0
                });
            }

            characterData.QuestObjectives[progress.QuestProgressID] = objectiveStates;

            Console.WriteLine($"[Quest] Character {characterId} started quest {questId} with {objectiveStates.Count} objectives");
            return true;
        }
        public async Task UpdateObjectiveProgressAsync(int characterId, int questId, ObjectiveType type, int targetId, int amount = 1)
        {
            await LoadCharacterQuestsAsync(characterId);
            if (!_characterQuests.TryGetValue(characterId, out var characterData)) return;

            if (!characterData.ActiveQuests.TryGetValue(questId, out var progress)) return;
            if (!characterData.QuestObjectives.TryGetValue(progress.QuestProgressID, out var objectives)) return;

            var obj = objectives.FirstOrDefault(o =>
                o.Original.Type == (int)type &&
                o.Original.TargetID == targetId &&
                !o.IsComplete);

            if (obj == null) return;

            obj.TempAmount = Math.Min(obj.TempAmount + amount, obj.Original.RequiredAmount);

        }

        //  Đánh dấu quest ready để complete (PendingReward)
        public async Task<bool> SetQuestPendingRewardAsync(int characterId, int questId)
        {
            await LoadCharacterQuestsAsync(characterId);
            if (!_characterQuests.TryGetValue(characterId, out var characterData)) return false;

            if (!characterData.ActiveQuests.TryGetValue(questId, out var progress)) return false;
            if (!characterData.QuestObjectives.TryGetValue(progress.QuestProgressID, out var objectives)) return false;

            // Kiểm tra tất cả objectives đã hoàn thành chưa
            if (objectives.Any(o => !o.IsComplete)) return false;

            // Cập nhật status thành PendingReward thay vì Completed
            progress.Status = "PendingReward";
            await _repository.UpdateQuestProgressAsync(progress);

            // Flush objective progress vào DB
            foreach (var obj in objectives)
            {
                obj.Original.CurrentAmount = obj.TempAmount;
                await _repository.UpdateQuestObjectiveAsync(obj.Original);
            }

            Console.WriteLine($"[Quest] {characterId} quest {questId} is now pending reward");
            return true;
        }

        // 👉 MODIFIED: Hoàn thành quest thực sự (khi client confirm)
        public async Task<bool> CompleteQuestAsync(int characterId, int questId)
        {
            await LoadCharacterQuestsAsync(characterId);
            if (!_characterQuests.TryGetValue(characterId, out var characterData)) return false;

            if (!characterData.ActiveQuests.TryGetValue(questId, out var progress)) return false;

            // Chỉ cho phép complete nếu quest đang ở trạng thái PendingReward
            if (progress.Status != "PendingReward") return false;

            // Cập nhật thành Completed
            progress.Status = "Completed";
            progress.CompletionTime = DateTime.Now;
            await _repository.UpdateQuestProgressAsync(progress);

            // Remove khỏi active và add vào completed
            characterData.ActiveQuests.Remove(questId);
            characterData.QuestObjectives.Remove(progress.QuestProgressID);
            characterData.CompletedQuests.Add(questId);

            Console.WriteLine($"[Quest] {characterId} đã hoàn thành Quest {questId}");
            return true;
        }

        public async Task FlushQuestProgressAsync(int characterId)
        {
            if (!_characterQuests.TryGetValue(characterId, out var characterData)) return;

            foreach (var list in characterData.QuestObjectives.Values)
            {
                foreach (var obj in list)
                {
                    if (obj.IsDirty)
                    {
                        obj.Original.CurrentAmount = obj.TempAmount;
                        await _repository.UpdateQuestObjectiveAsync(obj.Original);
                    }
                }
            }

            Console.WriteLine($"[Quest] Đã flush progress cho character {characterId}");
        }

        public void UnloadCharacterQuests(int characterId)
        {
            _characterQuests.Remove(characterId);
            Console.WriteLine($"[Quest] Unloaded data for character {characterId}");
        }

        public async Task<List<Quest>> GetAvailableQuestsAsync(int characterId, int level)
        {
            await LoadCharacterQuestsAsync(characterId);
            var characterData = _characterQuests[characterId];

            return _cacheService.GetQuestsByMinLevel(level)
                .Where(q => !characterData.CompletedQuests.Contains(q.QuestID) &&
                            !characterData.ActiveQuests.ContainsKey(q.QuestID))
                .ToList();
        }

        public async Task<List<Quest>> GetActiveQuestsAsync(int characterId)
        {
            await LoadCharacterQuestsAsync(characterId);
            var characterData = _characterQuests[characterId];

            return characterData.ActiveQuests.Values
                .Select(p => _cacheService.GetQuest(p.QuestID))
                .Where(q => q != null)
                .ToList();
        }

        // 👉 NEW: Lấy quests đang chờ nhận thưởng
        public async Task<List<Quest>> GetPendingRewardQuestsAsync(int characterId)
        {
            await LoadCharacterQuestsAsync(characterId);
            var characterData = _characterQuests[characterId];

            return characterData.ActiveQuests.Values
                .Where(p => p.Status == "PendingReward")
                .Select(p => _cacheService.GetQuest(p.QuestID))
                .Where(q => q != null)
                .ToList();
        }

        public async Task<(Questprogress progress, List<QuestObjectiveState> objectives)> GetQuestProgressAsync(int characterId, int questId)
        {
            await LoadCharacterQuestsAsync(characterId);
            if (!_characterQuests.TryGetValue(characterId, out var characterData)) return (null, null);

            if (!characterData.ActiveQuests.TryGetValue(questId, out var progress)) return (null, null);
            var objectives = characterData.QuestObjectives.GetValueOrDefault(progress.QuestProgressID);
            return (progress, objectives);
        }

        // 👉 MODIFIED: Không tự động complete, chỉ set PendingReward
        public async Task HandleMonsterKillAsync(int characterId, int killedMonsterId)
        {
            if (!_characterQuests.TryGetValue(characterId, out var characterData))
                return;

            foreach (var kvp in characterData.ActiveQuests)
            {
                var questId = kvp.Key;
                var progress = kvp.Value;

                // Skip quest đã pending reward
                if (progress.Status == "PendingReward") continue;

                if (!characterData.QuestObjectives.TryGetValue(progress.QuestProgressID, out var objectives))
                    continue;

                bool updated = false;

                foreach (var obj in objectives)
                {
                    if (obj.Original.Type == (int)ObjectiveType.KillMonster &&
                        obj.Original.TargetID == killedMonsterId &&
                        !obj.IsComplete)
                    {
                        obj.TempAmount = Math.Min(obj.TempAmount + 1, obj.Original.RequiredAmount);
                        updated = true;

                        Console.WriteLine($"[Quest] {characterId} tiến độ nhiệm vụ {questId} - {obj.TempAmount}/{obj.Original.RequiredAmount}");

                        if (obj.IsComplete)
                        {
                            Console.WriteLine($"[Quest] {characterId} hoàn thành 1 objective trong nhiệm vụ {questId}");
                        }
                    }
                }
                if (updated)
                {
                    var clientId = _sessionManager.GetClientIdByCharacterID(characterId);
                    if (clientId != null)
                    {
                        var progressData = new
                        {
                            questId = questId,
                            objectives = objectives.Select(o => new
                            {

                                Current = o.TempAmount,
                                Required = o.Original.RequiredAmount
                            })
                        };

                        var message = new BaseMessage
                        {
                            Action = "quest_progress_update",
                            Data = JObject.FromObject(progressData)
                        };

                        await _server.SendAsync(clientId.Value, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)));
                    }
                }

                // 👉 CHANGED: Nếu tất cả objectives hoàn thành thì set PendingReward, không complete
                if (updated && objectives.All(o => o.IsComplete))
                {
                    await SetQuestPendingRewardAsync(characterId, questId);

                    // Gửi thông báo quest ready to complete
                    var ClientId = _sessionManager.GetClientIdByCharacterID(characterId);
                    if (ClientId != null)
                    {
                        var message = new BaseMessage
                        {
                            Action = "quest_ready_to_complete", // 👈 Action mới
                            Data = new JObject
                            {
                                ["questId"] = questId,
                                ["message"] = "Nhiệm vụ đã hoàn thành! Hãy quay về NPC để nhận thưởng."
                            }
                        };
                        await _server.SendAsync(ClientId.Value, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)));
                    }
                }
            }
        }

        public bool IsQuestCompleted(int characterId, int questId)
        {
            if (!_characterQuests.TryGetValue(characterId, out var characterData)) return false;
            return characterData.CompletedQuests.Contains(questId);
        }

        // 👉 NEW: Kiểm tra quest có đang pending reward không
        public bool IsQuestPendingReward(int characterId, int questId)
        {
            if (!_characterQuests.TryGetValue(characterId, out var characterData)) return false;
            if (!characterData.ActiveQuests.TryGetValue(questId, out var progress)) return false;
            return progress.Status == "PendingReward";
        }

        public Task<Quest?> GetNextQuestAsync(int currentQuestId)
        {
            var nextQuest = _cacheService.GetQuest(currentQuestId + 1);
            return Task.FromResult<Quest?>(nextQuest);
        }

        public async Task<bool> CompleteAndChainNextAsync(int characterId, int questId)
        {
            var completed = await CompleteQuestAsync(characterId, questId);
            if (!completed) return false;

            var nextQuest = await GetNextQuestAsync(questId);
            if (nextQuest == null) return true;

            await StartQuestAsync(characterId, nextQuest.QuestID);

            var clientId = _sessionManager.GetClientIdByCharacterID(characterId);
            if (clientId != null)
            {
                var message = new BaseMessage
                {
                    Action = "quest_next",
                    Data = JObject.FromObject(new
                    {
                        quest = nextQuest
                    })
                };
                await _server.SendAsync(clientId.Value, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)));
            }

            Console.WriteLine($"[Quest] {characterId} nhận nhiệm vụ tiếp theo {nextQuest.QuestID}");
            return true;
        }
    }
}