using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Gameserver.core.Repo.Context;
using WatsonWebsocket;
using Microsoft.EntityFrameworkCore;
using Gameserver.core.Dto;
using Gameserver.core.Repo;
using Gameserver.core.Repo.Interfaces;
using Gameserver.core.Services.Interfaces;
using Gameserver.core.Network;
using Gameserver.core.Handler;
using Gameserver.core.Services;
using Gameserver.core.Manager;

namespace Gameserver.core
{

   
        public class ServerHostWrapper
        {
            private IHost? _host;
            private static bool _isRunning = false;
            public bool IsRunning => _isRunning;

            public async Task<bool> StartAsync()
            {
                if (_isRunning) return true;

                try
                {
                    _host = Host.CreateDefaultBuilder()
                        .ConfigureAppConfiguration((hostingContext, config) =>
                        {
                            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                        })
                        .ConfigureServices((context, services) =>
                        {
                            var serverSettings = context.Configuration.GetSection("ServerSettings").Get<ServerSettings>();
                            services.Configure<ServerSettings>(context.Configuration.GetSection("ServerSettings"));

                            var wsServer = new WatsonWsServer(serverSettings.Host, serverSettings.Port, false);
                            Console.WriteLine($"Port = {serverSettings.Port},Host = {serverSettings.Host}");

                            services.AddSingleton(wsServer);

                            var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
                            services.AddDbContext<AppDbContext>(options =>
                                options.UseMySql(connectionString, new MySqlServerVersion(new Version(10, 4, 32))));

                            // Existing repositories
                            services.AddScoped<IAccountRepo, AccountRepo>();
                            services.AddScoped<IMonsterRepo, MonsterRepo>();
                            services.AddScoped<ICharacterRepo, CharacterRepo>();
                            services.AddScoped<ISkillRepo, SkillRepo>();
                            services.AddScoped<INPCRepo, NPCRepo>();
                            services.AddScoped<IItemDataRepo, ItemDataRepo>();
                            services.AddScoped<IInventoryRepo, InventoryRepo>();

                            // Add Quest repository
                            services.AddScoped<IQuestRepository, QuestRepository>();

                            // Existing services
                            services.AddScoped<IAccountService, AccountService>();
                            services.AddScoped<ICharacterService, CharacterService>();

                            // Add Quest services
                            //services.AddScoped<IQuestService, QuestService>();
                            services.AddSingleton<QuestCacheService>();
                            services.AddSingleton<QuestManager>();

                            // Existing handlers
                            services.AddSingleton<IMessageHandler, LoginHandler>();
                            services.AddSingleton<IMessageHandler, LogoutHandler>();
                            services.AddSingleton<IMessageHandler, RegisterHandler>();
                            services.AddSingleton<IMessageHandler, CreateCharacterHandler>();
                            services.AddSingleton<IMessageHandler, GetCharInMap>();
                            services.AddSingleton<IMessageHandler, CharacterMoveHandler>();
                            services.AddSingleton<IMessageHandler, CharacterJoinMap>();
                            services.AddSingleton<IMessageHandler, LeaverMap>();
                            services.AddSingleton<IMessageHandler, CharacterJump>();
                            services.AddSingleton<IMessageHandler, GetMonsterInMapHandler>();
                            services.AddSingleton<IMessageHandler, UpdateCharacter>();
                            services.AddSingleton<IMessageHandler, PlayerAttackHandle>();
                            services.AddSingleton<IMessageHandler, PlayerRespawnHandle>();
                            services.AddSingleton<IMessageHandler, TalkQuestHandler>();
                            services.AddSingleton<IMessageHandler, GetInventoryHandle>();
                            services.AddSingleton<IMessageHandler, UseItemHandle>();
                            services.AddSingleton<IMessageHandler, BuyItemHandle>();
                            services.AddSingleton<IMessageHandler, PickupItemHandle>();



                            // Add Quest handlers
                            //services.AddSingleton<IMessageHandler, GetAvailableQuestsHandler>();
                            //services.AddSingleton<IMessageHandler, StartQuestHandler>();
                            //services.AddSingleton<IMessageHandler, CompleteQuestHandler>();
                            //services.AddSingleton<IMessageHandler, GetActiveQuestsHandler>();

                            services.AddSingleton<MessageDispatcher>();
                            services.AddSingleton<WebSocketHandler>();
                            services.AddSingleton<SessionManager>();
                            services.AddSingleton<MonsterManager>();
                            services.AddSingleton<SkillManager>();
                            services.AddSingleton<ItemDataManager>();
                            services.AddHostedService<GameServerHostedService>();
                        })
                        .Build();

                    await _host.StartAsync();
                    _isRunning = true;

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi khởi động server: {ex.Message}");
                    _isRunning = false;
                    return false;
                }
            }

            public async Task StopAsync()
            {
                if (!_isRunning || _host == null) return;

                await _host.StopAsync();
                _host.Dispose();
                _isRunning = false;
            }
        }
    }


