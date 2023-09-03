const { SlashCommandBuilder, EmbedBuilder } = require('discord.js');

module.exports = {
    data: new SlashCommandBuilder()
        .setName('sheet')
        .setDescription('Brings up a link to the Race Control Notice Board'),
        async execute(interaction){
            await interaction.reply('You can find the sheet here: \n' + process.env.SHEET_URL);
        }
}