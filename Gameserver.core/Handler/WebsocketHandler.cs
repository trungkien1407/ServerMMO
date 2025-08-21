using Gameserver.core.Dto;
using Gameserver.core.Network;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using WatsonWebsocket;

public class WebSocketHandler
{
    private readonly WatsonWsServer _server;
    private readonly IServiceProvider _serviceProvider;
    private readonly MessageDispatcher _messageDispatcher;
    private readonly SessionManager _sessionManager;  // Inject SessionManager
    private ConcurrentQueue<(Guid ClientId, string Message)> _messageQueue = new();
    private bool _running = true;

    public WebSocketHandler(IServiceProvider serviceProvider, MessageDispatcher messageDispatcher, SessionManager sessionManager)
    {
        _serviceProvider = serviceProvider;
        _messageDispatcher = messageDispatcher;
        _sessionManager = sessionManager;  // Injected SessionManager
        _server = serviceProvider.GetRequiredService<WatsonWsServer>(); // Get WatsonWsServer from DI
        _server.ClientConnected += OnClientConnected;
        _server.ClientDisconnected += OnClientDisconnected;
        _server.MessageReceived += OnMessageReceived;
        _sessionManager.DuplicateLoginDetected += OnDuplicateLoginDetected;
    }

    public void Start()
    {
        _server.Start();

        if (_server.IsListening)
            Console.WriteLine($"✅ Watson WebSocket Server is listening on port 14445");
        else
            Console.WriteLine($"❌ Failed to start server on port 14445");

        Task.Run(ProcessMessages);
    }


    private async void OnDuplicateLoginDetected(object? sender, DuplicateLoginEventArgs args)
    {
        var warning = new BaseMessage
        {
            Action = "duplicate_login",
            Data = new Newtonsoft.Json.Linq.JObject
            {
                ["message"] = "Tai Khoan Da dang nhap o noi khac"
            }
        };

        var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(warning));

        // Gửi thông báo cho cả client cũ và mới
        if (_server.IsClientConnected(args.ExistingClientId))
            await _server.SendAsync(args.ExistingClientId, payload);

        if (_server.IsClientConnected(args.NewClientId))
            await _server.SendAsync(args.NewClientId, payload);

        // Ngắt kết nối cả hai client
        _server.DisconnectClient(args.ExistingClientId);
        _server.DisconnectClient(args.NewClientId);
    }

    private void OnClientConnected(object sender, ConnectionEventArgs e)
    {
        Console.WriteLine($"Client {e.Client.Guid} connected");
    }

    private void OnClientDisconnected(object sender, DisconnectionEventArgs e)
    {
        Console.WriteLine($"Client {e.Client.Guid} disconnected");

        // Cleanup session when client disconnects
        _sessionManager.SaveData(e.Client.Guid);
        _sessionManager.RemoveSessionByClientId(e.Client.Guid);
    }

    private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        var message = Encoding.UTF8.GetString(e.Data.ToArray());
        _messageQueue.Enqueue((e.Client.Guid, message)); // Enqueue message for processing
    }

    private async Task ProcessMessages()
    {
        while (_running)
        {
            while (_messageQueue.TryDequeue(out var item))
            {
                await HandleMessage(item.ClientId, item.Message);
            }
            await Task.Delay(10); // Avoid 100% CPU usage
        }
    }

    private async Task HandleMessage(Guid clientId, string raw)
    {
        try
        {
            var baseMessage = JsonConvert.DeserializeObject<BaseMessage>(raw);
            // Dispatch the message to the appropriate handler
            await _messageDispatcher.Dispatch(clientId, baseMessage, _server);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling message: {ex.Message}");
        }
    }

    public async Task StopAsync()
    {
        _running = false;

        // Gửi cảnh báo trước 1 phút cho tất cả client
        var payload = new BaseMessage
        {
            Action = "bao_tri",
            Data = new Newtonsoft.Json.Linq.JObject
            {
                ["message"] = "Server sẽ bảo trì sau 1 phút, vui lòng thoát game."
            }
        };

        await BroadcastMessageAsync(payload);
        Console.WriteLine("🛑 Đã gửi cảnh báo bảo trì đến tất cả client. Đợi 1 phút...");
        
        await Task.Delay(TimeSpan.FromMinutes(1)); // Chờ 1 phút

        var clients = _server.ListClients();

        foreach (var client in clients)
        {
            if (_server.IsClientConnected(client.Guid))
            {
                await Task.Run(() => _server.DisconnectClient(client.Guid));
            }
        }

        Console.WriteLine("🔌 Tất cả client đã bị ngắt kết nối.");

        await Task.Delay(2000); // Đợi thêm để đảm bảo ổn định

        _server.Stop();
        Console.WriteLine("🧯 Server đã dừng.");
    }

    public async Task BroadcastMessageAsync(BaseMessage message)
    {
        var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
        var clients = _server.ListClients();

        var tasks = clients
            .Where(client => _server.IsClientConnected(client.Guid))
            .Select(client => _server.SendAsync(client.Guid, payload));

        await Task.WhenAll(tasks);
    }

    public async Task BroadcastToMap(int mapId, BaseMessage message)
    {
        try
        {
            var clientIds = _sessionManager.GetClientIdsInMap(mapId);
            if (clientIds == null || clientIds.Count == 0) return;

            var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

            var tasks = clientIds
                .Where(clientId => _server.IsClientConnected(clientId))
                .Select(clientId => _server.SendAsync(clientId, payload));

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BroadcastToMap] Error while broadcasting to map {mapId}: {ex.Message}");
        }
    }




    public void DisconnectClient(Guid clientId)
    {
         _server.DisconnectClient(clientId);
    }
    //public async Task BroadcastToMap(int mapId, BaseMessage message)
    //{
    //    var clientIds = _sessionManager.GetClientIdsInMap(mapId);
    //    var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
    //    var tasks = clientIds
    // .Where(cli => _server.IsClientConnected(id))
    // .Select(id => _server.SendAsync(id, payload));

    //    await Task.WhenAll(tasks);

    //}



}
