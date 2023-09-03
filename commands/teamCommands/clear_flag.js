const { SlashCommandBuilder, EmbedBuilder } = require('discord.js');

module.exports = {
    data: new SlashCommandBuilder()
        .setName('clear_black_flag')
        .setDescription('Use to request a black flag to be cleared')
        .addIntegerOption(option =>
            option
                .setName('number')
                .setDescription('What car number is requesting the clear?')
                .setRequired(true)
        ).addIntegerOption(option =>
            option
                .setName('lap')
                .setDescription('What lap did you get the black flag?')
                .setRequired(true)
        ).addStringOption(option =>
            option
                .setName('reason')
                .setDescription('What is the reason for the black flag?')
                .setRequired(true)
                ),
        async execute(interaction) {
            await interaction.deferReply();
            const number = interaction.options.getInteger('number');
            const lap = interaction.options.getInteger('lap');
            const reason = interaction.options.getString('reason');
            const eEmbed = new EmbedBuilder()
		        .setColor('#00FF00')
		        .setTitle('New Black Flag Clear Request')
		        .setDescription(`${interaction.user} is requesting a cleared black flag ${interaction.channel}`)
                .addFields(
                    {name: 'Car involved', value: number.toString(), inline: true},
                    {name: 'Lap', value: lap.toString(), inline: true},
                    {name: 'Reason for BF', value: reason, inline: true},
                ).setTimestamp();
            const otherEmbed = new EmbedBuilder()
		        .setColor('#00FF00')
		        .setTitle('You requested a black flag clear')
		        .setDescription(`Below is the information you submitted`)
                .addFields(
                    {name: 'Car involved', value: number.toString(), inline: true},
                    {name: 'Lap', value: lap.toString(), inline: true},
                    {name: 'Reason for BF', value: reason, inline: true},
                ).setTimestamp();
            await interaction.guild.channels.cache.get(process.env.BF_CLEAR_CHANNEL_ID).send({embeds: [eEmbed], content:'@here'});
            await interaction.editReply({content: 'Clear successfully requested', embeds: [otherEmbed]});
    },
};


