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

        console.log("defer");
        await interaction.deferReply();

        const channel = interaction.channel;
        const permissions = channel.permissionOverwrites.cache;
        const visibleRoleIds = permissions.filter(
            perm => perm.type === 0 && perm.allow.has(PermissionFlagsBits.ViewChannel) && !excludedRoleIds.includes(perm.id)
        );
        console.log("roles size");
        console.log(visibleRoleIds.size);
        if (visibleRoleIds.size === 0) {
            return interaction.editReply('No eligible team role found in this channel. Contact @Joscha Maier');
        }
        console.log("got roles size");
        const teamRoleId = visibleRoleIds[0];
        console.log(teamRoleId);
        const teamRole = interaction.guild.roles.cache.get(teamRoleId);
        console.log(teamRoleId);
        if (!teamRole) {
            return interaction.editReply('Team role could not be resolved.');
        }

        // fetch members with the role
        const members = await interaction.guild.members.fetch({ role: teamRoleId });
        const membersWithRole = Array.from(members.values()).map(m => m.toString());

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
