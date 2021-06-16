﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Google.Cloud.Firestore;

namespace RRBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireItemAttribute : PreconditionAttribute
    {
        public string ItemType { get; }

        public RequireItemAttribute(string itemType = "") => ItemType = itemType;

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/users").Document(context.Message.Author.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            if (snap.TryGetValue("items", out List<string> items))
            {
                if (string.IsNullOrEmpty(ItemType)) return PreconditionResult.FromSuccess();
                return items.Any(item => item.EndsWith(ItemType, StringComparison.Ordinal))
                    ? PreconditionResult.FromSuccess()
                    : PreconditionResult.FromError($"{context.Message.Author.Mention}, you need to have a {ItemType}.");
            }

            return PreconditionResult.FromError($"{context.Message.Author.Mention}, you have no items!");
        }
    }
}