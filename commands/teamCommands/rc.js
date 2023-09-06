const { SlashCommandBuilder, EmbedBuilder } = require('discord.js');

module.exports = {
    data: new SlashCommandBuilder()
        .setName('rc')
        .setDescription('Use to send a message to race control')
        .addStringOption(option =>
            option
            .setName('message')
            .setDescription('The message you want to send')
            .setRequired(true)
        ),
        async execute(interaction){
            await interaction.deferReply();
            const message = interaction.options.getString('message');
            const eEmbed = new EmbedBuilder()
		    .setColor('#E67E22')
		    .setTitle('Your Message')
		    .setDescription(message)
            .setTimestamp();
            await interaction.guild.channels.cache.get(process.env.RACE_CONTROL_CHANNEL_ID).send({content: `@here  ${interaction.user} sent a message in ${interaction.channel}:\n \n` + message});
            await interaction.editReply({ content: 'Your message has been sent to Race Control. They will reach out to you shortly if needed', embeds:  [eEmbed]});
        }
}