using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silk.Core.Database;
using Silk.Core.Services;
using Silk.Core.Tools.EventHelpers;
using Silk.Core.Utilities;

namespace Silk.Core
{

    public class Bot : IHostedService
    {
        //TODO: Fix all these usages, because they should be pulling from ctx.Services if possible. //
        public DiscordShardedClient Client          { get; set; }
        public static Bot? Instance                 { get; private set; }
        public static string DefaultCommandPrefix   { get; } = "s!";
        public static Stopwatch CommandTimer        { get; } = new();
        public SilkDbContext SilkDBContext          { get; private set; }
        
        public CommandsNextConfiguration? Commands  { get; private set; }

        private readonly IServiceProvider _services;
        private readonly ILogger<Bot> _logger;
        private readonly BotEventSubscriber _eventSubscriber;
        private readonly PrefixCacheService _prefixService;
        private readonly Stopwatch _sw = new();
        
        
        public Bot(IServiceProvider services, DiscordShardedClient client,
            ILogger<Bot> logger, BotEventSubscriber eventSubscriber,
            MessageAddedHelper messageAHelper, MessageRemovedHelper messageRHelper, 
            GuildAddedHelper guildAddedHelper, /* GuildRemovedHelper guildRemovedHelper,*/
            MemberRemovedHelper memberRemovedHelper,
            RoleAddedHelper roleAddedHelper, RoleRemovedHelper roleRemovedHelper, 
            
            IDbContextFactory<SilkDbContext> dbFactory)
        {
            
            _sw.Start();
            _services = services;
            _logger = logger;
            _eventSubscriber = eventSubscriber;

            client.MessageCreated += messageAHelper.Commands;
            client.MessageCreated += messageAHelper.Tickets;

            client.MessageDeleted += messageRHelper.OnRemoved;

            client.GuildMemberRemoved += memberRemovedHelper.OnMemberRemoved;
            
            client.GuildDownloadCompleted += guildAddedHelper.OnGuildDownloadComplete;
            client.GuildAvailable += guildAddedHelper.OnGuildAvailable;
            client.GuildCreated += guildAddedHelper.OnGuildJoin;
               

            client.GuildMemberUpdated += roleAddedHelper.CheckStaffRole;
            client.GuildMemberUpdated += roleRemovedHelper.CheckStaffRoles;

            SilkDBContext = dbFactory.CreateDbContext();
            Instance = this;
            Client = client;
        }

        private void InitializeCommands()
        {
           
            var sw = Stopwatch.StartNew();
            
            foreach (DiscordClient shard in Client.ShardClients.Values)
                shard.GetCommandsNext().RegisterCommands(Assembly.GetExecutingAssembly());
            

            sw.Stop();
            _logger.LogDebug($"Registered commands for {Client.ShardClients.Count} shards in {sw.ElapsedMilliseconds} ms.");
        }

        private async Task InitializeClientAsync()
        {
            Commands = new CommandsNextConfiguration
            {
                UseDefaultCommandHandler = false,
                Services = _services,
                IgnoreExtraArguments = true
                
            };
            
            await Client.StartAsync();
            
            await Client.UseCommandsNextAsync(Commands);
            InitializeCommands();
            
            await Client.UseInteractivityAsync(new InteractivityConfiguration
            {
                PaginationBehaviour = PaginationBehaviour.WrapAround,
                PaginationDeletion = PaginationDeletion.DeleteMessage,
                PollBehaviour = PollBehaviour.KeepEmojis,
                Timeout = TimeSpan.FromMinutes(1),
            });
            
            _eventSubscriber.CreateHandlers();
            
            var cmdNext = await Client.GetCommandsNextAsync();
            foreach (CommandsNextExtension c in cmdNext.Values)
            {
                c.SetHelpFormatter<HelpFormatter>(); 
                c.RegisterConverter(new MemberConverter());
            }
            
            _logger.LogInformation("Client Initialized.");
            _logger.LogInformation($"Startup time: {DateTime.Now.Subtract(Program.Startup).Seconds} seconds.");
            
            Client.Ready += async (_, _) => _logger.LogInformation("Client ready to process commands.");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await InitializeClientAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Client.StopAsync();
        }

    }
}