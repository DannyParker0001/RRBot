﻿using System;
using System.Threading.Tasks;
using Discord.Commands;
using Google.Cloud.Firestore;

namespace RRBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireNsfwEnabledAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/config").Document("modules");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            return snap.TryGetValue("nsfw", out bool nsfwEnabled) && nsfwEnabled
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError($"{context.Message.Author.Mention}, NSFW commands are disabled!");
        }
    }
}