﻿using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Floofbot.Modules
{
    [Summary("Commands for listing available commands")]
    [Discord.Commands.Name("Help")]
    public class Help : InteractiveBase
    {
        private static readonly Color EMBED_COLOR = Color.DarkGreen;
        private readonly IServiceProvider _serviceProvider;
        private readonly CommandService _commandService;
        public Help(CommandService commandService, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _commandService = commandService;
        }

        [Summary("Show all available commands")]
        [Command("help")]
        public async Task HelpCommand()
        {
            var modules = _commandService.Modules.ToList();
            var moduleCommands = new Dictionary<string, List<CommandInfo>>();

            foreach (ModuleInfo module in modules)
            {
                moduleCommands.Add(module.Name, new List<CommandInfo>());
            }
            
            foreach (CommandInfo command in _commandService.Commands)
            {
                moduleCommands[command.Module.Name].Add(command);
            }

            var fields = new List<EmbedFieldBuilder>();
            var pages = new List<PaginatedMessage.Page>();
            
            foreach (var module in modules)
            { 
                foreach (var command in moduleCommands[module.Name])
                {
                    if (string.IsNullOrEmpty(command.Name)) continue;
                    
                    var userMeetsCommandPreconditions = await command.CheckPreconditionsAsync(Context);

                    if (!userMeetsCommandPreconditions.IsSuccess) continue;
                    
                    var aliases = string.Empty;
                    var aliasesWithoutCommandName = command.Aliases.Where(x => !x.Contains(command.Name)).ToArray(); // remove the cmd name/group from aliases

                    if (aliasesWithoutCommandName != null && aliasesWithoutCommandName.Any()) // is not null or empty
                        aliases = "(aliases: " + string.Join(", ", aliasesWithoutCommandName) + ")";

                    fields.Add(new EmbedFieldBuilder()
                    {
                        Name = $"{command.Name} {aliases}",
                        Value = command.Summary ?? "No command description available",
                        IsInline = false
                    });
                }

                if (fields.Count <= 0) continue;
                
                pages.Add(new PaginatedMessage.Page
                {
                    Author = new EmbedAuthorBuilder { Name = module.Name },
                    Fields = new List<EmbedFieldBuilder>(fields),
                    Description = module.Summary ?? "No module description available"
                });
                    
                fields.Clear();
            }

            var message = Context.User.Mention;
            
            await PostHelpPages(message, pages);
        }

        [Summary("Show help for a specific set of commands (a module)")]
        [Command("help")]
        [Name("help <module name>")]
        public async Task HelpCommand([Summary("module name")] string requestedModule)
        {
            var moduleNames = _commandService.Modules.Select(x => x.Name.ToLower()).ToList();
            
            if (!moduleNames.Contains(requestedModule.ToLower()))
            {
                await Context.Channel.SendMessageAsync($"Unable to find commands available for '{requestedModule}'");
                return;
            }

            var moduleCommands = _commandService.Commands
                .Where(command => command.Module.Name.ToLower() == requestedModule.ToLower());

            var fields = new List<EmbedFieldBuilder>();
            var pages = new List<PaginatedMessage.Page>();
            
            foreach (var cmd in moduleCommands)
            {
                var userMeetsCommandPreconditions = await cmd.CheckPreconditionsAsync(Context);

                if (!userMeetsCommandPreconditions.IsSuccess) continue;
                
                foreach (ParameterInfo param in cmd.Parameters)
                {
                    fields.Add(new EmbedFieldBuilder()
                    {
                        Name = param.Name,
                        Value = param.Summary ?? "No parameter description available",
                        IsInline = false
                    });
                }
                
                pages.Add(new PaginatedMessage.Page
                {
                    Author = new EmbedAuthorBuilder { Name = cmd.Name },
                    Fields = new List<EmbedFieldBuilder>(fields),
                    Description = cmd.Summary ?? "No command description available"
                });
                
                fields.Clear();
            }

            if (pages.Count > 0)
            {
                var message = $"{Context.User.Mention} here are the commands available for '{requestedModule}'!";

                await PostHelpPages(message, pages);
            }
            // If we did not enter the if-statement:
            // User doesnt meet preconditions to use any of the commands in the module
        }

        private async Task PostHelpPages(string message, List<PaginatedMessage.Page> pages)
        {
            var pager = new PaginatedMessage
            {
                Pages = pages,
                Color = EMBED_COLOR,
                Content = message,
                FooterOverride = null,
                ThumbnailUrl = Context.Client.CurrentUser.GetAvatarUrl(),
                Options = PaginatedAppearanceOptions.Default,
                TimeStamp = DateTimeOffset.UtcNow
            };
            
            await PagedReplyAsync(pager, new ReactionList
            {
                Forward = true,
                Backward = true,
                Jump = true,
                Trash = true
            });
        }
    }
}
