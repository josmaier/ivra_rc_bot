const { SlashCommandBuilder, EmbedBuilder } = require('discord.js');

module.exports = {
    data: new SlashCommandBuilder()
        .setName('team')
        .setDescription('Send Message to a specific channel')
        .addChannelOption(option => 
            option
            .setName('channel')
            .setDescription('Name of the channel you want to send the message to')
            .setRequired(true)
        ).addStringOption(option =>
            option
            .setName('message')
            .setDescription('The message you want to sent')
            .setRequired(true)
        ),
        async execute(interaction){
            await interaction.deferReply();
            const channel = interaction.options.getChannel('channel');
            const message = interaction.options.getString('message');
            const eEmbed = new EmbedBuilder()
		    .setColor('#E67E22')
		    .setTitle('Race control sent a message')
		    .setDescription(message)
            .setTimestamp();
            const otherEmbed = new EmbedBuilder()
		    .setColor('#E67E22')    
		    .setTitle('Your message')
		    .setDescription(message)
            .setTimestamp();
            await channel.send({embeds: [eEmbed]});
            await interaction.editReply({ content: 'Your message has been sent', embeds:[otherEmbed]});
        }
}