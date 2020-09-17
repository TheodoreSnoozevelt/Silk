﻿using SilkBot.Exceptions;

namespace SilkBot
{
    using DSharpPlus;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;
    using DSharpPlus.Interactivity;
    using DSharpPlus.Interactivity.Enums;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;
    using SilkBot.Commands.Bot;
    using SilkBot.Commands.Economy;
    using SilkBot.Commands.Moderation.Utilities;
    using SilkBot.Models;
    using SilkBot.Server;
    using SilkBot.Tools;
    using SilkBot.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Defines the <see cref="Bot" />.
    /// </summary>
    public class Bot
    {
        public static Bot Instance { get; } = new Bot();

        public SilkDbContext SilkDBContext { get; set; } = new SilkDbContext();

        public static Stopwatch CommandTimer { get; } = new Stopwatch();

        [JsonProperty(PropertyName = "Guild Prefixes")]
        public static Dictionary<ulong, string> GuildPrefixes { get; set; }

        public static string SilkDefaultCommandPrefix { get; } = "!";


        public DiscordClient Client { get; set; }


        public CommandsNextConfiguration Commands { get; } = new CommandsNextConfiguration { CaseSensitive = false, EnableDefaultHelp = true, EnableMentionPrefix = true };


        public InteractivityConfiguration Interactivity { get; }


        public TimerBatcher Timer { get; } = new TimerBatcher();

        private Bot()
        {
            sw.Start();
        }


        private readonly Stopwatch sw = new Stopwatch();


        public async Task RunBotAsync()
        {
            await SilkDBContext.Database.MigrateAsync();
            await InitializeClient();
            await Task.Delay(-1);
        }


        private Task OnCommandErrored(CommandErrorEventArgs e)
        {
            switch (e.Exception)
            {
                case InsufficientFundsException _:
                    e.Context.Channel.SendMessageAsync(e.Exception.Message);
                    break;
                default:
                    break;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// The event fired when the bot can see a guild.
        /// </summary>
        /// <param name="eventArgs">The event arguments passed when a guild <see cref="GuildCreateEventArgs"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task OnGuildAvailable(GuildCreateEventArgs eventArgs)
        {
            var guild = await CreateGuildOnNullAsync(eventArgs.Guild.Id);
            if (!SilkDBContext.Guilds.Contains(guild)) SilkDBContext.Guilds.Add(guild);
            await CacheStaffMembers(guild, eventArgs.Guild.Members.Values);
            await SilkDBContext.SaveChangesAsync();

            //TODO: Fix Logger
            
            //eventArgs.Client.DebugLogger.LogMessage(LogLevel.Info, "Silk!", $"Guild available: {eventArgs.Guild.Name}", DateTime.Now);
        }


        /// <summary>
        /// Cache staff members.
        /// </summary>
        public async Task CacheStaffMembers(Guild guild, IEnumerable<DiscordMember> members)
        {
            var staffMembers = members
                .AsQueryable()
                .Where(member => member.HasPermission(Permissions.KickMembers) && !member.IsBot)
                .Select(staffMember => new DiscordUserInfo {Guild = guild, UserId = staffMember.Id, Flags = UserFlag.Staff});


            guild.DiscordUserInfos.AddRange(staffMembers);
            await SilkDBContext.SaveChangesAsync();
        }

        /// <summary>
        /// The GetGuildAsync.
        /// </summary>
        /// <param name="guildId">The guildId<see cref="ulong"/>.</param>
        /// <returns><see cref="Task{Guild}"/>.</returns>
        public async Task<Guild> CreateGuildOnNullAsync(ulong guildId)
        {
            var guild = await SilkDBContext.Guilds.FirstOrDefaultAsync(g => g.DiscordGuildId == guildId);
            
            if (guild != null)
            {
                return guild;
            }

            guild = new Guild { DiscordGuildId = guildId, Prefix = SilkDefaultCommandPrefix };
            return guild;
        }



        /// <summary>
        /// The OnReady.
        /// </summary>
        /// <param name="e">The e<see cref="ReadyEventArgs"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private Task OnReady(ReadyEventArgs e)
        {
            //TODO: Fix Logger
            //e.Client.DebugLogger.LogMessage(LogLevel.Info, "Silk!", "Ready to process events.", DateTime.Now);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Register valid commands.
        /// </summary>
        private void RegisterCommands() => Client.GetCommandsNext().RegisterCommands(Assembly.GetExecutingAssembly());

        /// <summary>
        /// Client initialization method; prepare the bot and load requisite data.
        /// </summary>
        private async Task InitializeClient()
        {
            var token = File.ReadAllText("./Token.txt");
            var config = new DiscordConfiguration
            {
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Information,
                Token = token,
                TokenType = TokenType.Bot,
                
            };



            Client = new DiscordClient(config);

            Client.UseInteractivity(new InteractivityConfiguration { PaginationBehaviour = PaginationBehaviour.WrapAround, Timeout = TimeSpan.FromMinutes(1)});

            Client.UseCommandsNext(Commands);

            // Register database context and apply newest migration if not already done.

            RegisterCommands();

            HelpCache.Initialize(DiscordColor.Azure);
            //Data.PopulateDataOnApplicationLoad();
            //All these handlers do is subscribe to the bot's appropriate event, and do something, hence not assigning a variable to it.

            Client.Ready += OnReady;
            Client.GuildAvailable += OnGuildAvailable;
            Client.GetCommandsNext().CommandErrored += OnCommandErrored;


            Client.GuildDownloadCompleted += async (e) =>
            {
                //TODO: Fix Logger
                //Client.DebugLogger.LogMessage(LogLevel.Info, "Silk!", $"Available guilds: {e.Guilds.Count}", DateTime.Now);
                //await Data.FetchGuildInfo(Client.Guilds.Values);
            };


            await Client.ConnectAsync();
            new MessageDeletionHandler(Client);
            new MessageEditHandler(Client);
            new GuildJoinHandler();
            new MessageCreationHandler();
            new GuildMemberCountChangeHandler(Client);
            sw.Stop();
            Console.WriteLine($"Startup Time: {sw.ElapsedMilliseconds} ms", ConsoleColor.Blue);
        }


    }
}