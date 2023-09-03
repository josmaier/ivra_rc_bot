const { SlashCommandBuilder, EmbedBuilder} = require('discord.js');

module.exports ={
    data: new SlashCommandBuilder()
        .setName('served')
        .setDescription('Use to notify Race control that you served a penalty')
        .addIntegerOption(option =>
            option
                .setName('number')
                .setDescription('What is your car number?')
                .setRequired(true)
        ).addIntegerOption(option =>
            option
                .setName('inc_number')
                .setDescription('Please enter the incident number you received the penalty for')
                .setRequired(true)
        ).addIntegerOption(option =>
            option
                .setName('lap_number')
                .setDescription('Please provide the lap number when you served the penalty')
                .setRequired(true)
        ),
    async execute(interaction){
        await interaction.deferReply();
        const number = interaction.options.getInteger('number');
        const inc_number = interaction.options.getInteger('number');
        const lap_number = interaction.options.getInteger('number');
        const eEmbed = new EmbedBuilder()
		    .setColor('#00FF00')
		    .setTitle('Penalty Served')
		    .setDescription(`#` + number.toString() + ` served a penalty`)
            .addFields(
                {name: 'Incident Number', value: inc_number.toString(), inline: true},
                {name: 'Origin Car', value: number.toString(), inline: true},
                {name: 'Lap Served', value: lap_number.toString(), inline: true},
                {name: 'Channel', value:  `${interaction.channel}`, inline: true},
            ).setTimestamp();
        const otherEmbed = new EmbedBuilder()
		    .setColor('#00FF00')
		    .setTitle('Notification successfully submitted')
		    .setDescription(`Thank you ${interaction.user}, we will confirm your notification shortly. Please check the race control sheet for the status of your penalty.`)
            .addFields(
                {name: 'Notification details', value:  `Below you can find the information you submitted:`, inline: false},
                {name: 'Incident Number', value: inc_number.toString(), inline: true},
                {name: 'Lap number', value: lap_number.toString(), inline: true},
                {name: 'Car Number', value: number.toString(), inline: true},
            ).setTimestamp();
            await interaction.guild.channels.cache.get(process.env.SERVED_CHANNEL_ID).send({embeds: [eEmbed], content:'@here'});
            await interaction.editReply({embeds: [otherEmbed]});
    }
}