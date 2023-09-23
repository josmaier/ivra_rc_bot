const { SlashCommandBuilder, EmbedBuilder } = require('discord.js');

module.exports = {
    data: new SlashCommandBuilder()
        .setName('info')
        .setDescription('Displays information about the bot'),
        async execute(interaction){
            const eEmbed = new EmbedBuilder()
		    .setColor('#00FF00')
		    .setTitle('IVRA RC Bot')
		    .setDescription(`Information about the IVRA RC bot`)
            .setAuthor({ name : 'Joscha Maier', iconURL: 'https://cdn.discordapp.com/avatars/177096206235729921/c867e1ffd4f3ef8f4f7da452e0071779.webp?size=128'})
            
            .addFields(
                {name: 'Version', value: '1.3', inline: false},
                {name: 'Framework', value: 'Node', inline: false},
                {name: 'Deployed on and with', value: 'Hetzner using PM2', inline: false},
                {name: 'Ugly Code on git', value: 'https://github.com/josmaier/ivra_rc_bot', inline: false},
                {name: 'Version history', value: `Changelog:
                \n	1.0 First Deployment
                \n  1.1 Fixed Protest command, elaborated error messages, added info command
                \n  1.2 Added embeds to /team and /rc to display the send message
                \n  1.3 Added Ability to ping roles to /team`, inline: false}
            ).setTimestamp();
            await interaction.reply({embeds: [eEmbed]});
        }
}