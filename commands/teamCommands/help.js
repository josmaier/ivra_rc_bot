const { SlashCommandBuilder, EmbedBuilder } = require('discord.js');

module.exports = {
    data: new SlashCommandBuilder()
        .setName('help')
        .setDescription('List all available commands'),
        async execute(interaction){
            await interaction.reply(`Here is a list of commands for you to use:
            \t\`/rc\` - Use if you need to talk with Race Control.
            \t\`/help\` - Use to bring up a list of commands.
            \t\`/sheet\` - Use to bring up the Race Control Decision Sheet link.
            \t\`/protest\` - Use to log a new protest.
            \t\`/served\` - Use to notify Race control that you served a penalty.
            \t\`/clear\` - Use to request a cleared black flag.
            \t\`/tow\` - Use to request a tow.`);
        }
}