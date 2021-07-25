﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Floofbot.Configs;
using System.Text.RegularExpressions;
using UnitsNet;

namespace Floofbot
{
    [Summary("Utility commands")]
    [Name("Utilities")]
    public class Utilities : InteractiveBase
    {
        private static readonly Discord.Color EMBED_COLOR = Color.Magenta;

        [Command("ping")]
        [Summary("Responds with the ping in milliseconds")]
        public async Task Ping()
        {
            var sw = Stopwatch.StartNew();
            var msg = await Context.Channel.SendMessageAsync(":owl:").ConfigureAwait(false);
            sw.Stop();
            await msg.DeleteAsync();

            EmbedBuilder builder = new EmbedBuilder()
            {
                Title = "Butts!",
                Description = $"📶 Reply: `{(int)sw.Elapsed.TotalMilliseconds}ms`",
                Color = EMBED_COLOR
            };

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("userinfo")]
        [Summary("Displays information on a mentioned user. If no parameters are given, displays the user's own information")]
        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task UserInfo(IGuildUser usr = null)
        {
            var user = usr ?? Context.User as IGuildUser;

            if (user == null)
                return;

            string avatar = "https://cdn.discordapp.com/attachments/440635657925165060/442039889475665930/Turqouise.jpg";

            // Get user's Discord joining date and time, in UTC
            string discordJoin = user.CreatedAt.ToUniversalTime().ToString("dd\\/MMM\\/yyyy \\a\\t H:MM \\U\\T\\C");
            // Get user's Guild joining date and time, in UTC
            string guildJoin = user.JoinedAt?.ToUniversalTime().ToString("dd\\/MMM\\/yyyy \\a\\t H:MM \\U\\T\\C");

            if (user.AvatarId != null)
                avatar = user.GetAvatarUrl(ImageFormat.Auto, 512);

            string infostring = $"👥 **User info for {user.Mention}** \n";
            infostring +=
                 $"**User** : {user.Nickname ?? user.Username} ({user.Username}#{user.Discriminator})\n" +
                 $"**ID** : {user.Id}\n" +
                 $"**Discord Join Date** : {discordJoin} \n" +
                 $"**Guild Join Date** : {guildJoin}\n" +
                 $"**Status** : {user.Status}\n";

            EmbedBuilder builder = new EmbedBuilder
            {
                ThumbnailUrl = avatar,
                Description = infostring,
                Color = EMBED_COLOR
            };

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("avatar")]
        [Summary("Displays a mentioned user's avatar. If no parameters are given, displays the user's own avatar")]
        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Avatar([Remainder] IGuildUser user = null)
        {
            if (user == null)
                user = (IGuildUser)Context.User;

            var avatarUrl = user.GetAvatarUrl(ImageFormat.Auto, 512);
            EmbedBuilder builder = new EmbedBuilder()
            {
                Description = $"🖼️ **Avatar for:** {user.Mention}\n",
                ImageUrl = avatarUrl,
                Color = EMBED_COLOR

            };
            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("embed")]
        [Summary("Repeats a message in an embed format")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task RepeatMessage([Remainder] string message =null)
        {
            if (message != null)
            {
                EmbedBuilder builder = new EmbedBuilder()
                {
                    Description = message,
                    Color = EMBED_COLOR
                };
                await Context.Channel.SendMessageAsync("", false, builder.Build());
                await Context.Channel.DeleteMessageAsync(Context.Message.Id);
            }
            else
            {
                await Context.Channel.SendMessageAsync("Usage: `.embed [message]`");
            }
        }

        [Command("echo")]
        [Summary("Repeats a text message directly")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task EchoMessage([Remainder] string message = null)
        {
            if (message != null)
            {
                await Context.Channel.SendMessageAsync(message);
                await Context.Channel.DeleteMessageAsync(Context.Message.Id);
            }
            else
            {
                await Context.Channel.SendMessageAsync("Usage: `.echo [message]`");
            }
        }

        [Command("serverinfo")]
        [Summary("Returns information about the current server")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task ServerInfo()
        {
            SocketGuild guild = Context.Guild;

            // Get Guild creation date and time, in UTC
            string guildCreated = guild.CreatedAt.ToUniversalTime().ToString("dd\\/MMM\\/yyyy \\a\\t H:MM \\U\\T\\C");

            int numberTextChannels = guild.TextChannels.Count;
            int numberVoiceChannels = guild.VoiceChannels.Count;
            int daysOld = Context.Message.CreatedAt.Subtract(guild.CreatedAt).Days;
            string daysAgo = $" That's " + ((daysOld == 0) ? "today!" : (daysOld == 1) ? $"yesterday!" : $"{daysOld} days ago!");
            string createdAt = $"Created {guildCreated}." + daysAgo;
            int totalMembers = guild.MemberCount;
            int onlineUsers = guild.Users.Where(mem => mem.Status == UserStatus.Online).Count();
            int numberRoles = guild.Roles.Count;
            int numberEmojis = guild.Emotes.Count;
            uint colour = (uint)new Random().Next(0x1000000); // random hex

            EmbedBuilder embed = new EmbedBuilder();

            embed.WithDescription(createdAt)
                 .WithColor(new Discord.Color(colour))
                 .AddField("Users (Online/Total)", $"{onlineUsers}/{totalMembers}", true)
                 .AddField("Text Channels", numberTextChannels, true)
                 .AddField("Voice Channels", numberVoiceChannels, true)
                 .AddField("Roles", numberRoles, true)
                 .AddField("Emojis", numberEmojis, true)
                 .AddField("Owner", $"{guild.Owner.Username}#{guild.Owner.Discriminator}", true)
                 .WithFooter($"Server ID: {guild.Id}")
                 .WithAuthor(guild.Name)
                 .WithCurrentTimestamp();

            if (Uri.IsWellFormedUriString(guild.IconUrl, UriKind.Absolute))
                embed.WithThumbnailUrl(guild.IconUrl);

            await Context.Channel.SendMessageAsync("", false, embed.Build());

        }


        [RequireOwner]
        [Command("reloadconfig")]
        [Summary("Reloads the config file")]
        public async Task ReloadConfig()
        {
            try
            {
                BotConfigFactory.Reinitialize();
            }
            catch (InvalidDataException e)
            {
                await Context.Channel.SendMessageAsync(e.Message);
                return;
            }

            if (BotConfigFactory.Config.Activity != null)
            {
                await Context.Client.SetActivityAsync(BotConfigFactory.Config.Activity);
            }
            await Context.Channel.SendMessageAsync("Config reloaded successfully");
        }

        [Command("serverlist")]
        [Summary("Returns a list of servers that the bot is in")]
        [RequireOwner]
        public async Task ServerList()
        {
            List<SocketGuild> guilds = new List<SocketGuild>(Context.Client.Guilds);
            List<PaginatedMessage.Page> pages = new List<PaginatedMessage.Page>();

            foreach (SocketGuild g in guilds)
            {
                List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();

                fields.Add(new EmbedFieldBuilder()
                {
                    Name = $"Owner",
                    Value = $"{g.Owner.Username}#{g.Owner.Discriminator} | ``{g.Owner.Id}``",
                    IsInline = false
                });
                fields.Add(new EmbedFieldBuilder()
                {
                    Name = $"Server ID",
                    Value = g.Id,
                    IsInline = false
                });
                fields.Add(new EmbedFieldBuilder()
                {
                    Name = $"Members",
                    Value = g.MemberCount,
                    IsInline = false
                });

                pages.Add(new PaginatedMessage.Page
                {
                    Author = new EmbedAuthorBuilder { Name = g.Name },
                    Fields = new List<EmbedFieldBuilder>(fields),
                    ThumbnailUrl = (Uri.IsWellFormedUriString(g.IconUrl, UriKind.Absolute) ? g.IconUrl : null)
                });
            }
            var pager = new PaginatedMessage
            {
                Pages = pages,
                Color = Color.DarkGreen,
                Content = "Here are a list of servers that I am in!",
                FooterOverride = null,
                Options = PaginatedAppearanceOptions.Default,
                TimeStamp = DateTimeOffset.UtcNow
            };
            await PagedReplyAsync(pager, new ReactionList
            {
                Forward = true,
                Backward = true,
                Jump = true,
                Trash = true
            }, true);
        }
    }
    [Summary("Conversion commands")]
    [Discord.Commands.Name("Conversions")]
    public class Conversions : InteractiveBase
    {
        private static readonly Discord.Color EMBED_COLOR = Color.Magenta;

        [Command("convert")]
        [Alias("conv")]
        [Summary("Converts units to other units, such as Celcius to Fahrenheit.")]

        public async Task convert(string input)
        {
            Regex fahReg = new Regex("\\b(\\d+)(f)\\b", RegexOptions.IgnoreCase);
            Regex celReg = new Regex("\\b(\\d+)(c)\\b", RegexOptions.IgnoreCase);
            Regex miReg = new Regex("\\b(\\d+)(mi)\\b", RegexOptions.IgnoreCase);
            Regex kmReg = new Regex("\\b(\\d+)(km)\\b", RegexOptions.IgnoreCase);
            Regex kgReg = new Regex("\\b(\\d+)(kg)\\b", RegexOptions.IgnoreCase);
            Regex lbReg = new Regex("\\b(\\d+)(lbs)\\b", RegexOptions.IgnoreCase);

            if (fahReg.Match(input).Success)
            {
                Match m = fahReg.Match(input);

                Group g = m.Groups[1];

                string fahStr = Convert.ToString(g);
                double fahTmp = Convert.ToDouble(fahStr);

                Temperature Fah = Temperature.FromDegreesFahrenheit(fahTmp);
                double Cel = Fah.DegreesCelsius;

                EmbedBuilder builder = new EmbedBuilder()
                {
                    Title = "Temperature conversion",
                    Description = $"🌡 {(Temperature)Fah} is equal to {(double)Math.Round(Cel, 2, MidpointRounding.ToEven)}°C.",
                    Color = EMBED_COLOR
                };

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            else if (celReg.Match(input).Success)
            {
                Match m = celReg.Match(input);

                Group g = m.Groups[1];

                string celStr = Convert.ToString(g);
                double celTmp = Convert.ToDouble(celStr);

                Temperature Cel = Temperature.FromDegreesCelsius(celTmp);
                double Fah = Cel.DegreesFahrenheit;

                EmbedBuilder builder = new EmbedBuilder()
                {
                    Title = "Temperature conversion",
                    Description = $"🌡 {(Temperature)Cel} is equal to {(double)Math.Round(Fah, 2, MidpointRounding.ToEven)}°F.",
                    Color = EMBED_COLOR
                };

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            else if (miReg.Match(input).Success)
            {
                Match m = miReg.Match(input);

                Group g = m.Groups[1];

                string miStr = Convert.ToString(g);
                double miTmp = Convert.ToDouble(miStr);

                Length mi = Length.FromMiles(miTmp);
                double km = mi.Kilometers;

                EmbedBuilder builder = new EmbedBuilder()
                {
                    Title = "Length conversion",
                    Description = $"📏 {(Length)mi} is equal to {(double)Math.Round(km, 3, MidpointRounding.ToEven)}Km.",
                    Color = EMBED_COLOR
                };

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            else if (kmReg.Match(input).Success)
            {
                Match m = kmReg.Match(input);

                Group g = m.Groups[1];

                string kmStr = Convert.ToString(g);
                double kmTmp = Convert.ToDouble(kmStr);

                Length km = Length.FromKilometers(kmTmp);
                double mi = km.Miles;

                EmbedBuilder builder = new EmbedBuilder()
                {
                    Title = "Length conversion",
                    Description = $"📏 {(Length)km} is equal to {(double)Math.Round(mi, 3, MidpointRounding.ToEven)}mi.",
                    Color = EMBED_COLOR
                };

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            else if (kgReg.Match(input).Success)
            {
                Match m = kgReg.Match(input);

                Group g = m.Groups[1];

                string kgStr = Convert.ToString(g);
                double kgTmp = Convert.ToDouble(kgStr);


                Mass kg = Mass.FromKilograms(kgTmp);
                double lb = kg.Pounds;

                EmbedBuilder builder = new EmbedBuilder()
                {
                    Title = "Mass conversion",
                    Description = $"⚖️ {(Mass)kg} is equal to {(double)Math.Round(lb, 3, MidpointRounding.ToEven)}lbs.",
                    Color = EMBED_COLOR
                };

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            else if (lbReg.Match(input).Success)
            {
                Match m = lbReg.Match(input);

                Group g = m.Groups[1];

                string lbStr = Convert.ToString(g);
                double lbTmp = Convert.ToDouble(lbStr);

                Mass lb = Mass.FromPounds(lbTmp);
                double kg = lb.Kilograms;

                EmbedBuilder builder = new EmbedBuilder()
                {
                    Title = "Mass conversion",
                    Description = $"⚖️ {(Mass)lb} is equal to {(double)Math.Round(kg, 3, MidpointRounding.ToEven)}Kg.",
                    Color = EMBED_COLOR
                };

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            else
            {
                EmbedBuilder builder = new EmbedBuilder()
                {
                    Title = "Conversion module",
                    Description = $"No unit has been entered, or it was not recognized. Available units are mi<->km, °C<->F, and kg<->lbs.",
                    Color = EMBED_COLOR
                };

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
        }
    }
}
