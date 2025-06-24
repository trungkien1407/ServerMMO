
using Gameserver.core;


var server = new ServerHostWrapper();
await server.StartAsync();

Console.WriteLine("Nhấn 'q' để thoát");
while (Console.ReadKey(true).Key != ConsoleKey.Q) ;
await server.StopAsync();