const { SlashCommandBuilder, EmbedBuilder } = require('discord.js');

module.exports = {
    data: new SlashCommandBuilder()
        .setName('team')
        .setDescription('Send Message to a specific channel')
        .addChannelOption(option => 
            option
            .setName('channel')
            .setDescription('Name of the channel you want to send the message too')
            .setRequired(true)
        ).addStringOption(option =>
            option
            .setName('message')
            .setDescription('The message you want to send')
            .setRequired(true)
        ),
        async execute(interaction){
            await interaction.deferReply();
            const channel = interaction.options.getChannel('channel');
            const message = interaction.options.getString('message');
            const eEmbed = new EmbedBuilder()
		    .setColor('#E67E22')
		    .setTitle('Race control send a message')
		    .setDescription(message)
            .setTimestamp();
            await channel.send({embeds: [eEmbed]});
            await interaction.editReply({ content: 'Your message has been send'});
        }
}