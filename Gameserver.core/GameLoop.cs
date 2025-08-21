using System;
using System.Threading;
using System.Threading.Tasks;
using Gameserver.core.Network;
using Gameserver.core.Services;

public class GameLoop
{
    private readonly MonsterManager _monsterManager;
    private readonly SessionManager _sessionManager;
    private Timer? _gameTimer;
    private Timer? _cleanupTimer;
    private readonly object _tickLock = new();
    private long _tickCount = 0;
    private DateTime _lastTickTime = DateTime.UtcNow;
    private readonly SemaphoreSlim _tickSemaphore = new(1, 1);


    public GameLoop(MonsterManager monsterManager, SessionManager sessionManager)
    {
        _monsterManager = monsterManager;
        _sessionManager = sessionManager;
    }

    public void Start()
    {
        Console.WriteLine("Starting Game Loop...");

        // Main game tick (every 500ms)
        _gameTimer = new Timer(async _ => await Tick(), null, 0,1000);

        // Cleanup tick (every 30 seconds)
        _cleanupTimer = new Timer(async _ => await CleanupTick(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public void Stop()
    {
        Console.WriteLine("Stopping Game Loop...");
        _gameTimer?.Dispose();
        _cleanupTimer?.Dispose();
    }

    private async Task Tick()
    {
        if (!await _tickSemaphore.WaitAsync(0)) // Không chờ nếu đang chạy
        {
            Console.WriteLine("Skipping tick - previous tick still running");
            return;
        }

        try
        {
            var startTime = DateTime.UtcNow;
            _tickCount++;

            await _monsterManager.TickMonsters();

            var tickDuration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            if (tickDuration > 100)
            {
                Console.WriteLine($"[WARN] Slow tick #{_tickCount}: {tickDuration:F2}ms");
            }

            if (_tickCount % 10 == 0)
            {
                var avgTickTime = (DateTime.UtcNow - _lastTickTime).TotalMilliseconds / 10;
                Console.WriteLine($"Tick #{_tickCount} - Avg: {avgTickTime:F1}ms");
                _lastTickTime = DateTime.UtcNow;
            }

            if (_tickCount % 300 == 0)
            {
                Console.WriteLine("Autosaving player sessions...");
                await _sessionManager.Autosave();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Game tick exception: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        finally
        {
            _tickSemaphore.Release();
        }
    }

    private async Task CleanupTick()
    {
        try
        {
            Console.WriteLine("Running cleanup...");
           _monsterManager.CleanupInactiveMaps();

            // Optional: periodic GC
            if (_tickCount % 120 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Console.WriteLine("Garbage collection completed");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] CleanupTick exception: {ex.Message}");
        }
    }
}
