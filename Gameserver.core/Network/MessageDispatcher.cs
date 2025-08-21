using Gameserver.core.Dto;
using Gameserver.core.Handler;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace Gameserver.core.Network
{
    public class MessageDispatcher
    {
        private readonly Dictionary<string, IMessageHandler> _handlers;
        private readonly SessionManager _sessionManager;

        public MessageDispatcher(IEnumerable<IMessageHandler> handlers, SessionManager sessionManager)
        {
            _handlers = new Dictionary<string, IMessageHandler>(StringComparer.OrdinalIgnoreCase);
            _sessionManager = sessionManager;

            // Register all handlers
            foreach (var handler in handlers)
            {
                _handlers[handler.Action] = handler;
                Console.WriteLine($"Registered handler for action: {handler.Action}");
            }

            
        }

        public async Task Dispatch(Guid clientId, BaseMessage message, WatsonWsServer server)
        {
            if (message == null)
            {
                Console.WriteLine("Error: Received null message");
                return;
            }

            Console.WriteLine($"{clientId} Dispatching message with action: {message.Action}");

            if (_handlers.TryGetValue(message.Action, out var handler))
            {
                try
                {
                    await handler.Handle(clientId, message.Data, server);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in handler {message.Action}: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Unknown action: {message.Action}");
            }
        }
    }
}