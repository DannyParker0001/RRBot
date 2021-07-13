﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;
using RRBot.Extensions;
using RRBot.Preconditions;
using RRBot.Systems;

namespace RRBot.Modules
{
    public class Gambling : ModuleBase<SocketCommandContext>
    {
        private bool ThreeInARow(int[] results, int emoji)
        {
            return (results[0] == emoji && results[1] == emoji && results[2] == emoji) || (results[1] == emoji && results[2] == emoji && results[3] == emoji);
        }
        private bool TwoInARow(int[] results, int emoji)
        {
            return (results[0] == emoji && results[1] == emoji) || (results[1] == emoji && results[2] == emoji) || (results[2] == emoji && results[3] == emoji);
        }

        public static readonly Random random = new Random();
        public static readonly Emoji SEVEN = new Emoji("7️⃣");
        public static readonly Emoji APPLE = new Emoji("\uD83C\uDF4E");
        public static readonly Emoji GRAPES = new Emoji("\uD83C\uDF47");
        public static readonly Emoji CHERRIES = new Emoji("\uD83C\uDF52");
        public static readonly Dictionary<int, Emoji> emojis = new Dictionary<int, Emoji>
        {
            { 1, SEVEN },
            { 2, APPLE },
            { 3, GRAPES },
            { 4, CHERRIES }
        };

        private async Task<RuntimeResult> GenericGamble(float bet, double odds, float mult, bool exactRoll = false)
        {
            if (bet < 0f || float.IsNaN(bet)) return CommandResult.FromError($"{Context.User.Mention}, you can't bet nothing!");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            float cash = snap.GetValue<float>("cash");

            if (cash < bet) return CommandResult.FromError($"{Context.User.Mention}, you can't bet more than what you have!");

            double roll = Math.Round(random.NextDouble(1, 101), 2);
            bool success = !exactRoll ? roll >= odds : roll.CompareTo(odds) == 0;
            if (success)
            {
                float payout = bet * mult;
                await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash + payout);
                await Context.User.NotifyAsync(Context.Channel, $"Good shit my guy! You rolled a {roll} and got yourself **{payout.ToString("C2")}**!");
            }
            else
            {
                await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash - bet);
                await Context.User.NotifyAsync(Context.Channel, $"Well damn, you rolled a {roll}, which wasn't enough. You lost **{bet.ToString("C2")}**.");
            }

            return CommandResult.FromSuccess();
        }

        [Command("55x2")]
        [Summary("Roll 55 or higher on a 100 sided die, get 2x what you put in.")]
        [Remarks("``$55x2 [bet]``")]
        [RequireCash]
        public async Task<RuntimeResult> Roll55(float bet) => await GenericGamble(bet, 55, 1);

        [Command("69.69")]
        [Summary("Roll 69.69 on a 100 sided die, get 6969x what you put in.")]
        [Remarks("``$69.69 [bet]``")]
        [RequireCash]
        public async Task<RuntimeResult> Roll6969(float bet) => await GenericGamble(bet, 69.69, 6968f, true);

        [Command("75+")]
        [Summary("Roll 75 or higher on a 100 sided die, get 3.6x what you put in.")]
        [Remarks("``$75+ [bet]``")]
        [RequireCash]
        public async Task<RuntimeResult> Roll75(float bet) => await GenericGamble(bet, 75, 2.6f);

        [Command("99+")]
        [Summary("Roll 99 or higher on a 100 sided die, get 90x whatyou put in.")]
        [Remarks("``$99+ [bet]``")]
        [RequireCash]
        public async Task<RuntimeResult> Roll99(float bet) => await GenericGamble(bet, 99, 89f);

        [Command("double")]
        [Summary("Double your cash...?")]
        [Remarks("``$double``")]
        [RequireCash]
        public async Task<RuntimeResult> Double()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("usingSlots", out bool usingSlots) && usingSlots)
                return CommandResult.FromError($"{Context.User.Mention}, you appear to be currently gambling. I cannot do any transactions at the moment.");

            float cash = snap.GetValue<float>("cash");

            if (random.Next(0, 2) != 0) await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash * 2);
            else await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, 10);

            await Context.User.NotifyAsync(Context.Channel, "I have doubled your cash.");
            return CommandResult.FromSuccess();
        }

        [Command("slots")]
        [Summary("Take the slot machine for a spin!")]
        [Remarks("``$slots [bet]``")]
        [RequireCash]
        public async Task<RuntimeResult> Slots(float bet)
        {
            if (bet < 500f || float.IsNaN(bet)) return CommandResult.FromError($"{Context.User.Mention}, you can't bet less than $500!");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            float cash = snap.GetValue<float>("cash");
            if (cash < bet) return CommandResult.FromError($"{Context.User.Mention}, you can't bet more than what you have!");

            if (snap.TryGetValue("usingSlots", out bool usingSlots) && usingSlots) 
                return CommandResult.FromError($"{Context.User.Mention}, you are already using the slot machine!");
            await doc.SetAsync(new { usingSlots = true }, SetOptions.MergeAll);

            await Task.Factory.StartNew(async () =>
            {
                int[] results = new int[4];
                float payoutMult = 1f;

                EmbedBuilder embed = new EmbedBuilder
                {
                    Color = Color.Red,
                    Title = "Slots"
                };
                IUserMessage slotMsg = await ReplyAsync(embed: embed.Build());
                for (int i = 0; i < 5; i++)
                {
                    results[0] = random.Next(1, 5);
                    results[1] = random.Next(1, 5);
                    results[2] = random.Next(1, 5);
                    results[3] = random.Next(1, 5);

                    embed.WithDescription($"{emojis[results[0]]}{emojis[results[1]]}{emojis[results[2]]}{emojis[results[3]]}");
                    await slotMsg.ModifyAsync(msg => msg.Embed = embed.Build());

                    await Task.Delay(TimeSpan.FromSeconds(1.5));
                }

                int sevens = results.Count(num => num == 1);
                int apples = results.Count(num => num == 2);
                int grapes = results.Count(num => num == 3);
                int cherries = results.Count(num => num == 4);
                if (sevens == 4) payoutMult = 25f;
                else if (apples == 4 || grapes == 4 || cherries == 4) payoutMult = 5f;
                else if (ThreeInARow(results, 1)) payoutMult = 3f;
                else if (ThreeInARow(results, 2) || ThreeInARow(results, 3) || ThreeInARow(results, 4)) payoutMult = 2f;
                else
                {
                    if (TwoInARow(results, 1)) payoutMult += 0.5f;
                    if (TwoInARow(results, 2)) payoutMult += 0.25f;
                    if (TwoInARow(results, 3)) payoutMult += 0.25f;
                    if (TwoInARow(results, 4)) payoutMult += 0.25f;
                }

                if (payoutMult > 1f)
                {
                    float payout = (bet * payoutMult) - bet;
                    await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash + payout);

                    if (payoutMult == 25f)
                        await Context.User.NotifyAsync(Context.Channel, $"SWEET BABY JESUS, YOU GOT A MOTHERFUCKING JACKPOT! You won **{payout.ToString("C2")}**!");
                    else
                        await Context.User.NotifyAsync(Context.Channel, $"Nicely done! You won **{payout.ToString("C2")}** ({payoutMult}x your bet, minus the {bet.ToString("C2")} you put in).");
                }
                else
                {
                    await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash - bet);
                    await Context.User.NotifyAsync(Context.Channel, $"You won nothing! Well, you can't win 'em all. You lost **{bet.ToString("C2")}**.");
                }

                await doc.SetAsync(new { usingSlots = false }, SetOptions.MergeAll);
            });

            return CommandResult.FromSuccess();
        }
    }
}