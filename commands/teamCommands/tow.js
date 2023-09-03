const { SlashCommandBuilder, EmbedBuilder } = require('discord.js');

module.exports = {
    data: new SlashCommandBuilder()
        .setName('tow')
        .setDescription('Use to request a tow')
        .addIntegerOption(option =>
                option
                .setName('number')
                .setDescription('Please enter your Car Number')
                .setRequired(true)
        ),
    async execute(interaction){
        await interaction.deferReply();
        const number = interaction.options.getInteger('number');
        const eEmbed = new EmbedBuilder()
		    .setColor('#00FF00')
		    .setTitle('New Tow Request')
		    .setDescription(`${interaction.user} is requesting tow in ${interaction.channel}`)
            .addFields(
                {name: 'Origin Car', value: number.toString(), inline: true},
            ).setTimestamp();
        await interaction.guild.channels.cache.get(process.env.TOW_CHANNEL_ID).send({embeds: [eEmbed], content:'@here new tow request'});
        await interaction.editReply({content: 'Tow successfully requested.  Please wait for RC confirmation before you tow back to the pits!'});
    }
}