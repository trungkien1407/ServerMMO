using Gameserver.core.Network;
using Gameserver.core.Repo.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace Gameserver.core.Manager
{
    public class ItemDataManager
    {
        private Dictionary<int, CachedItemData> _itemDataCache = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly SessionManager _sessionManager;
        private readonly WatsonWsServer _watsonWsServer;

        public ItemDataManager(IServiceProvider serviceProvider, SessionManager sessionManager, WatsonWsServer watsonWsServer)
        {
            
            _serviceProvider = serviceProvider;
            _sessionManager = sessionManager;
            _watsonWsServer = watsonWsServer;
        }
        public async Task LoadItemDataAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var itemDataRepo = scope.ServiceProvider.GetRequiredService<IItemDataRepo>();
            var dataList = await itemDataRepo.GetItemDataAsync();

            _itemDataCache.Clear();
            foreach (var item in dataList)
            {
                _itemDataCache[item.ItemID] = item;
            }

            Console.WriteLine($"[ItemDataManager] Loaded {dataList.Count} item base data.");
        }
        public CachedItemData? GetItemById(int itemId)
        {
            _itemDataCache.TryGetValue(itemId, out var item);
            return item;
        }


    }
}
