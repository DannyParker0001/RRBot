using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RRBot.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace RRBot.Modules
{
    [Summary("Commands that don't do anything related to the bot's systems: they just exist for fun (hence the name).")]
    public class Fun : ModuleBase<SocketCommandContext>
    {
        private async Task<RuntimeResult> RandomImg(string apiUrl, string key, bool cat = false)
        {
            using WebClient client = new();
            string response = await client.DownloadStringTaskAsync(apiUrl);
            string image = "";
            if (cat)
            {
                JArray arr = JArray.Parse(response);
                image = arr[0][key].Value<string>();
            }
            else
            {
                JObject json = JObject.Parse(response);
                image = json[key].Value<string>();
            }

            if (string.IsNullOrWhiteSpace(image))
                return CommandResult.FromError("Couldn't find the picture you wanted :(");

            EmbedBuilder embed = new()
            {
                Color = Color.Red,
                Title = "Found one!",
                ImageUrl = image
            };
            await ReplyAsync(embed: embed.Build());
            return CommandResult.FromSuccess();
        }

        [Alias("gato", "kitty")]
        [Command("cat")]
        [Summary("Random cat picture!")]
        [Remarks("$cat")]
        public async Task<RuntimeResult> Cat() => await RandomImg("https://api.thecatapi.com/v1/images/search", "url", true);

        [Alias("definition")]
        [Command("define")]
        [Summary("Define a term.")]
        [Remarks("$define [term]")]
        public async Task<RuntimeResult> Define([Remainder] string term)
        {
            using WebClient client = new();
            string response = await client.DownloadStringTaskAsync($"https://api.pearson.com/v2/dictionaries/ldoce5/entries?headword={term}");
            DefinitionResponse def = JsonConvert.DeserializeObject<DefinitionResponse>(response);
            if (def.count == 0)
                return CommandResult.FromError("Couldn't find anything for that term, chief.");

            StringBuilder description = new();
            DefinitionResult[] filteredResults = def.results.Where(res => res.headword.Equals(term, StringComparison.OrdinalIgnoreCase)
                && res.senses != null).ToArray();
            for (int i = 1; i <= filteredResults.Length; i++)
            {
                DefinitionResult result = filteredResults[i - 1];
                description.AppendLine($"**{i}:**\n*{result.part_of_speech}*");
                foreach (Sense sense in result.senses)
                {
                    description.AppendLine($"Definition: {sense.definition[0]}");
                    if (sense.examples != null)
                        description.AppendLine($"Example: {sense.examples[0].text}");
                }
            }

            if (description.Length == 0)
                return CommandResult.FromError("Couldn't find anything for that term, chief.");
            
            EmbedBuilder embed = new()
            {
                Color = Color.Red,
                Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(term.ToLower()),
                Description = description.ToString()
            };

            await ReplyAsync(embed: embed.Build());
            return CommandResult.FromSuccess();
        }

        [Alias("doggo", "heckingchonker")]
        [Command("dog")]
        [Summary("Random dog picture!")]
        [Remarks("$dog")]
        public async Task<RuntimeResult> Dog() => await RandomImg("https://dog.ceo/api/breeds/image/random", "message");

        [Command("flip")]
        [Summary("Flip a coin.")]
        [Remarks("$flip")]
        public async Task Flip()
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Red,
                Title = "Coin"
            };

            if (RandomUtil.Next(0, 2) != 0)
            {
                embed.WithDescription("You flipped.. HEADS!");
                embed.WithImageUrl("https://images.squarespace-cdn.com/content/v1/5786a922cd0f688d44f9cab2/1482515593363-33KPMNHCMDW7G0T12VK9/image-asset.png");
            }
            else
            {
                embed.WithDescription("You flipped.. TAILS!");
                embed.WithImageUrl("https://i.imgur.com/LxajBRS.png");
            }

            await ReplyAsync(embed: embed.Build());
        }

        [Command("trivia")]
        [Summary("Generate a random trivia question.")]
        [Remarks("$trivia")]
        public async Task Trivia()
        {
            // get all the properties we need
            using WebClient client = new();
            string response = await client.DownloadStringTaskAsync("https://opentdb.com/api.php?amount=1");
            Trivia trivia = JsonConvert.DeserializeObject<Trivia>(response);
            string question = HttpUtility.HtmlDecode(trivia.results[0].question);
            string correctAnswer = "​" + HttpUtility.HtmlDecode(trivia.results[0].correct_answer);
            List<string> answers = new() { correctAnswer };
            foreach (string incorrect in trivia.results[0].incorrect_answers)
                answers.Add(HttpUtility.HtmlDecode(incorrect));

            // randomize answers array
            for (int i = 0; i < answers.Count - 1; i++)
            {
                int j = RandomUtil.Next(i, answers.Count);
                string temp = answers[i];
                answers[i] = answers[j];
                answers[j] = temp;
            }

            StringBuilder description = new($"{question}\n\nReact with the respective number to submit your answer.\n");
            for (int i = 1; i <= answers.Count; i++)
                description.AppendLine($"{i}: {answers[i-1]}");

            EmbedBuilder embed = new()
            {
                Color = Color.Red,
                Title = "Trivia!",
                Description = description.ToString()
            };
            await ReplyAsync(embed: embed.Build());
        }

        [Command("waifu")]
        [Summary("Get yourself a random waifu from our vast and sexy collection of scrumptious waifus.")]
        [Remarks("$waifu")]
        public async Task Waifu()
        {
            List<string> keys = Constants.WAIFUS.Keys.ToList();
            string waifu = keys[RandomUtil.Next(Constants.WAIFUS.Count)];

            EmbedBuilder waifuEmbed = new()
            {
                Color = Color.Red,
                Title = "Say hello to your new waifu!",
                Description = $"Your waifu is **{waifu}**.",
                ImageUrl = Constants.WAIFUS[waifu]
            };

            await ReplyAsync(embed: waifuEmbed.Build());
        }
    }
}
