﻿using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Silk.Core.Services
{
    public sealed class DiscordEmojiCreationService
    {
        private readonly DiscordClient _client;

        public DiscordEmojiCreationService(DiscordClient client)
        {
            _client = client;
        }

        public DiscordEmoji GetEmoji(string name)
        {
            return DiscordEmoji.FromName(_client, name);
        }

        public IEnumerable<DiscordEmoji> GetEmoji(params string[] names)
        {
            foreach (string emojiName in names) yield return DiscordEmoji.FromName(_client, emojiName);
        }
    }
}