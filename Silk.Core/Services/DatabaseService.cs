﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Silk.Core.Database;
using Silk.Core.Database.Models;

namespace Silk.Core.Services
{
    /// <summary>
    /// A service class that provides a bridge between <see cref="SilkDbContext"/> and Model classes such as <see cref="UserModel"/> & <see cref="GuildModel"/>
    ///
    /// This class also provides methods for updating models, and handles Database operations, abstracting it from command classes.
    /// </summary>
    public class DatabaseService
    {
        // This is the only instance of an IDbContextFactory<T> we should need. //
        private readonly IDbContextFactory<SilkDbContext> _dbFactory;

        public DatabaseService(IDbContextFactory<SilkDbContext> dbFactory) => _dbFactory = dbFactory;

        public async Task<GuildModel> GetGuildAsync(ulong guildId)
        {
            await using SilkDbContext db = _dbFactory.CreateDbContext();
            return await db.Guilds.Include(g => g.Configuration).Include(g => g.Users).FirstOrDefaultAsync(g => g.Id == guildId);
        }

        public async Task<UserModel?> GetGuildUserAsync(ulong guildId, ulong? userId)
        {
            if (!userId.HasValue) return null;
            
            await using SilkDbContext db = _dbFactory.CreateDbContext();
            
            GuildModel guild = await db.Guilds.Include(g => g.Users).FirstOrDefaultAsync(g => g.Id == guildId);
            UserModel? user = guild.Users.FirstOrDefault(u => u.Id == userId);

            user ??= new() {Id = userId.Value};
            await db.SaveChangesAsync();
            
            return user;
        }
        

        /// <summary>
        /// Update a <see cref="UserModel"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task UpdateGuildUserAsync(UserModel user, Action<UserModel> updateAction)
        {
            await using SilkDbContext db = _dbFactory.CreateDbContext();
            UserModel? trackedUser = await db.Users.FirstOrDefaultAsync(u => u == user);
            if (trackedUser is null) throw new ArgumentNullException($"{nameof(user)} cannot be null.");
            updateAction(trackedUser);
            await db.SaveChangesAsync();
        }

        public async Task<UserModel> GetOrAddUserAsync(ulong guildId, ulong userId)
        {
            await using SilkDbContext db = _dbFactory.CreateDbContext();
            GuildModel guild = await db.Guilds.Include(g => g.Users).FirstOrDefaultAsync(g => g.Id == guildId);
            UserModel? user = guild.Users.FirstOrDefault(u => u.Id == userId);
            user ??= new() {Id = userId};
            await db.SaveChangesAsync();
            return user;
        }
        
        public async Task RemoveUserAsync(UserModel user)
        {
            await using SilkDbContext db = _dbFactory.CreateDbContext();
            GuildModel guild = await db.Guilds.FirstAsync(g => g == user.Guild);
            UserModel? trackedUser = guild.Users.FirstOrDefault(u => u == user);
            Log.Logger.Verbose("Grabbed user");
            if (trackedUser is null) return;
            Log.Logger.Verbose("User was not null");
            trackedUser.Guild.Users.Remove(trackedUser);
            await db.SaveChangesAsync();
        }

        


    }
}