using Discord.Interactions;

namespace RaceControlBot.Commands
{
    public class HelpCommand()
        : InteractionModuleBase<SocketInteractionContext>
    {

        [SlashCommand("help", "Shows a list of command")]

    }
}
