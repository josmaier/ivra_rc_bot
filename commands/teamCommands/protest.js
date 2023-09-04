const { SlashCommandBuilder, EmbedBuilder} = require('discord.js');

module.exports = {
    data: new SlashCommandBuilder()
        .setName('protest')
        .setDescription('Use to log a new protest')
        .addIntegerOption(option =>
            option
                .setName('number')
                .setDescription('What is your car number?')
                .setRequired(true)
        ).addStringOption(option =>
            option
                .setName('numbers_involved')
                .setDescription('What are the car numbers of other cars involved?')
                .setRequired(true)
        ).addStringOption(option =>
            option
                .setName('timestamp')
                .setDescription('What is the iRacing Time Stamp (HR:MM:SS)')
                .setRequired(true)
        ).addStringOption(option =>
            option
                .setName('description')
                .setDescription('Please provide a short description of the incident:')
                .setRequired(true)
        ),
    async execute(interaction){
        await interaction.deferReply();
        const number = interaction.options.getInteger('number');
        const numbers_involved = interaction.options.getString('numbers_involved');
        const timestamp = interaction.options.getString('timestamp');
        const description = interaction.options.getString('description');
        const eEmbed = new EmbedBuilder()
		    .setColor('#00FF00')
		    .setTitle('New protest')
		    .setDescription(`${interaction.user}  submitted a protest in ${interaction.channel}`)
            .addFields(
                {name: 'Origin Car', value: number.toString(), inline: true},
                {name: 'Cars Involved', value: numbers_involved, inline: true},
                {name: 'Timestamp', value: timestamp, inline: true},
                {name: 'Description', value: description, inline: true},
            ).setTimestamp();
        const otherEmbed = new EmbedBuilder()
		    .setColor('#00FF00')
		    .setTitle('Protest successfully submitted')
		    .setDescription(`Thank you ${interaction.user}, your protest is successfully submitted. Please check the protest sheet for the status of your protest.`)
            .addFields(
                {name: 'Protest Details', value: 'Below you can find the information you submitted:', inline: false},
                {name: 'Origin Car', value: number.toString(), inline: true},
                {name: 'Cars Involved', value: numbers_involved, inline: true},
                {name: 'Timestamp', value: timestamp, inline: true},
                {name: 'Description', value: description, inline: true},
            ).setTimestamp();
            await interaction.guild.channels.cache.get(process.env.PROTEST_CHANNEL_ID).send({embeds: [eEmbed], content:'@here'});
            await interaction.editReply({content: 'New Protest', embeds: [otherEmbed]});
    }
}