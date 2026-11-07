const { SlashCommandBuilder, PermissionFlagsBits } = require('discord.js');
const { getSheetUrl, setSheetUrl } = require('../../utils/sheetLink');

module.exports = {
    data: new SlashCommandBuilder()
        .setName('set-sheet')
        .setDescription('Changes the link that /sheet returns')
        .addStringOption(option =>
            option
            .setName('link')
            .setDescription('The new link to the Race Control Notice Board')
            .setRequired(true)
        )
        .setDefaultMemberPermissions(PermissionFlagsBits.Administrator),
        async execute(interaction){
            const link = interaction.options.getString('link').trim();

            let url;
            try {
                url = new URL(link);
            } catch {
                url = null;
            }
            if (!url || (url.protocol !== 'http:' && url.protocol !== 'https:')) {
                await interaction.reply({ content: 'That is not a valid link. It has to start with http:// or https://', ephemeral: true });
                return;
            }

            const oldLink = getSheetUrl();
            setSheetUrl(link);
            await interaction.reply({ content: `The sheet link has been changed.\nOld: ${oldLink ?? 'none'}\nNew: ${link}`, ephemeral: true });
        }
}
