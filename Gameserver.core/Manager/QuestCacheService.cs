using Gameserver.core.Models;
using Gameserver.core.Repo.Interfaces;

public class QuestCacheService
{
    private static readonly Dictionary<int, Quest> _questCache = new();
    private static bool _isInitialized = false;
    private readonly IQuestRepository _repository;

    public QuestCacheService(IQuestRepository repository)
    {
        _repository = repository;
    }

    public async Task InitializeCacheAsync()
    {
        if (_isInitialized) return;

        var quests = await _repository.GetAllQuestsAsync();
        _questCache.Clear();

        foreach (var quest in quests)
        {
            _questCache[quest.QuestID] = quest;
        }

        _isInitialized = true;
        Console.WriteLine($"[QuestCache] Loaded {_questCache.Count} quests");
    }

    public Quest GetQuest(int questId) => _questCache.TryGetValue(questId, out var q) ? q : null;

    public List<Quest> GetQuestsByNPC(int npcId) =>
        _questCache.Values.Where(q => q.StartNPCID == npcId || q.EndNPCID == npcId).ToList();

    public List<Quest> GetQuestsByMinLevel(int level) =>
        _questCache.Values.Where(q => q.MinLevel <= level).ToList();

    public List<Quest> GetAllQuests() => _questCache.Values.ToList();
}
