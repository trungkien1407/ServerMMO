using Gameserver.core.Manager;
using Gameserver.core.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class GameServerHostedService : IHostedService
{
    private readonly WebSocketHandler _wsHandler;
    private readonly IServiceProvider _serviceProvider;
    private CancellationTokenSource _cts = new CancellationTokenSource();
    private GameLoop? _gameLoop;

    public GameServerHostedService(WebSocketHandler wsHandler, IServiceProvider serviceProvider)
    {
        _wsHandler = wsHandler;
        _serviceProvider = serviceProvider;
      
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _wsHandler.Start();
        // Console.WriteLine("Server đang chạy...");

        // Start listening for key press on a background task
        //   Task.Run(() => ListenForKeyPress(_cts.Token));


        // Lấy MonsterManager từ DI để tạo GameLoop
        var monsterManager = _serviceProvider.GetRequiredService<MonsterManager>();
        var questCache = _serviceProvider.GetRequiredService<QuestCacheService>();
        var ItemManager = _serviceProvider.GetRequiredService<ItemDataManager>();
       await questCache.InitializeCacheAsync();
        await ItemManager.LoadItemDataAsync();
    
        var sessionManager = _serviceProvider.GetRequiredService<SessionManager>();
        _gameLoop = new GameLoop(monsterManager,sessionManager);
        _gameLoop.Start();


      
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
   //     _cts.Cancel(); // Dừng vòng lặp ListenForKeyPress nếu đang chạy

        await _wsHandler.StopAsync();
     //   Console.WriteLine("Server đã dừng.");
    }

    private void ListenForKeyPress(CancellationToken token)
    {
        Console.WriteLine("Nhấn phím 'q' để tắt server...");

        while (!token.IsCancellationRequested)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true); // true để không hiện ký tự lên màn hình
                if (key.Key == ConsoleKey.Q)
                {
                    Console.WriteLine("Đang tắt server...");
                    StopAsync(CancellationToken.None).GetAwaiter().GetResult();
                    break;
                }
            }
            Thread.Sleep(100); // tránh loop quá nhanh
        }
    }
    public void BanPlayer()
    {

    }
}
