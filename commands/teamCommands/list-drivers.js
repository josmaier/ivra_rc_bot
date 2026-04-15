const { SlashCommandBuilder, PermissionFlagsBits, EmbedBuilder } = require('discord.js');

module.exports = {
    data: new SlashCommandBuilder()
        .setName('list-team-drivers')
        .setDescription('Shows all users with the team role for this channel.')
        .setDefaultMemberPermissions(PermissionFlagsBits.SendMessages),

    async execute(interaction) {
        const managerRoleId = '1131530691159531550';
        const excludedRoleIds = [
            '307229025552564235',
            '341286151920812033',
            '318989503090130944',
            '343396972033474560'
        ];

        const member = await interaction.guild.members.fetch(interaction.user.id);
        if (!member.roles.cache.has(managerRoleId)) {
            return interaction.reply({ content: 'You are not authorized to use this command.', ephemeral: true });
        }

        await interaction.deferReply();

        const channel = interaction.channel;
        const permissions = channel.permissionOverwrites.cache;

        const visibleRoleIds = permissions.filter(
            perm =>
                perm.type === 0 &&
                perm.allow.has(PermissionFlagsBits.ViewChannel) &&
                !excludedRoleIds.includes(perm.id)
        );

        if (visibleRoleIds.size === 0) {
            return interaction.editReply('No eligible team role found in this channel. Contact @Joscha Maier');
        }

        // .first() returns the first Collection entry (a PermissionOverwrite object)
        const teamRoleId = visibleRoleIds.first().id;
        const teamRole = interaction.guild.roles.cache.get(teamRoleId);

        if (!teamRole) {
            return interaction.editReply('Team role could not be resolved.');
        }

        // Fetch all members, then filter by role
        const allMembers = await interaction.guild.members.fetch();
        const membersWithRole = allMembers
            .filter(m => m.roles.cache.has(teamRoleId))
            .map(m => m.toString());

        const embed = new EmbedBuilder()
            .setTitle(`Members with ${teamRole.name}`)
            .setColor(0x2b2d31)
            .setDescription(
                membersWithRole.length > 0
                    ? membersWithRole.join('\n')
                    : 'No users currently have this role.'
            );

        await interaction.editReply({ embeds: [embed] });
    }
};