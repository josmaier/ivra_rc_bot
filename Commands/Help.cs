using Discord.Interactions;

namespace RaceControlBot.Commands
{
    public class HelpCommand()
        : InteractionModuleBase<SocketInteractionContext>
    {

        [SlashCommand("help", "Shows a list of command")]
        public async Task HelpAsync()
        {
            await DeferAsync();
            string helpText =
                @"Here is a list of commands for you to use:
                `/rc`     - Use if you need to talk with Race Control.
                `/help`   - Use to bring up a list of commands.
                `/sheet`  - Use to bring up the Race Control Decision Sheet link.
                `/protest`- Use to log a new protest.
                `/served` - Use to notify Race Control that you served a penalty.
                `/clear`  - Use to request a cleared black flag.
                `/tow`    - Use to request a tow.";

            await FollowupAsync(helpText);
        }
    }
}
