﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Silk.Core.Utilities;

namespace Silk.Core.Commands.General
{
    [Category(Categories.General)]
    public class DMCommand : BaseCommandModule
    {
        [Command("DM")]
        public async Task DM(CommandContext ctx, ulong guildId, DiscordUser user, [RemainingText] string message)
        {
            DiscordGuild guild = await ctx.Client.GetGuildAsync(guildId);
            KeyValuePair<ulong, DiscordMember> member = guild.Members.Single(pair => pair.Key == user.Id);

            await member.Value.SendMessageAsync(message);
        }
        
        [Hidden]
        [Command("DM")]
        public async Task DM(CommandContext ctx, DiscordUser user, [RemainingText] string message)
        {
            KeyValuePair<ulong, DiscordMember> member = ctx.Guild.Members.Single(pair => pair.Key == user.Id);
            await member.Value.SendMessageAsync(message);
        }


        public static async Task DM(CommandContext ctx, DiscordMember member, DiscordEmbed message)
        {
            await member.SendMessageAsync(embed: message);
        }
    }
}