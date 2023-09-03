const { SlashCommandBuilder, EmbedBuilder } = require('discord.js');

module.exports = {
    data: new SlashCommandBuilder()
        .setName('penalty')
        .setDescription('Send penalty to a team and the notice board')
        .addIntegerOption(option =>
            option
            .setName('time_amount')
            .setDescription('Penalty time Amount (if 0 = Drive Through')
            .setRequired(true)
        ).addStringOption(options =>
            option
            .setName('penalty_category')
            .setDescription('Category of the penalty')
            .setRequired(true)
        ).addIntegerOption(option =>
            option
            .setName('inc_number')
            .setDescription('Number of the incident')
            .setRequired(true)
        ).addIntegerOption(option =>
                option
                .setName('number')
                .setDescription('Number of the car that recieves the penalty')
                .setRequired(true)
        .addChannelOption(option =>
            option
            .setName('channel')
            .setDescription('Channel of the team recieving the penalty')
            .setRequired(true))
        .addRoleOption(option =>
            option
            .setName('role')
            .setDescription('Role of the team you want to ping')
            .require(true)
        ),
        async execute(interaction){
            await interaction.deferReply();
            const channel = interaction.options.getChannel('channel');
            if(interaction.options.getInteger('time_amount') === 0) {
                const time_amount = 'Drive Through';
            } else {
                const time_amount = interaction.options.getInteger('time_amount');
            }
            const penalty_category = interaction.options.getInteger('penalty_category');
            const inc_number = interaction.options.getInteger('inc_number');

            await channel.send({content: 'Race control send a message: \n' + message});
            await interaction.editReply({ content: 'Your message has been send'});
        }
}