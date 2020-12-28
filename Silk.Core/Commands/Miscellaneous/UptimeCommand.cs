﻿using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Humanizer;
using Humanizer.Localisation;
using Silk.Core.Utilities;

namespace Silk.Core.Commands.Miscellaneous
{
    [Category(Categories.Misc)]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class UptimeCommand : BaseCommandModule
    {
        [Command]
        public async Task UpTime(CommandContext ctx)
        {
            DateTime now = DateTime.Now;
            TimeSpan uptime = now.Subtract(Program.Startup);
            await ctx.RespondAsync($"Running for `{uptime.Humanize(4, null, TimeUnit.Month, TimeUnit.Second)}.`");
        }
    }
}