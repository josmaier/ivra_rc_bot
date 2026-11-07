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

        console.log('[list-team-drivers] Command triggered by', interaction.user.tag);

        const member = await interaction.guild.members.fetch(interaction.user.id);
        console.log('[list-team-drivers] Fetched invoking member:', member.user.tag);

        if (!member.roles.cache.has(managerRoleId)) {
            console.log('[list-team-drivers] Unauthorized - missing manager role');
            return interaction.reply({ content: 'You are not authorized to use this command.', ephemeral: true });
        }

        console.log('[list-team-drivers] Authorization passed, deferring reply...');
        await interaction.deferReply();
        console.log('[list-team-drivers] Reply deferred');

        const channel = interaction.channel;
        console.log('[list-team-drivers] Channel:', channel.name, '| ID:', channel.id);

        const permissions = channel.permissionOverwrites.cache;
        console.log('[list-team-drivers] Raw permission overwrites:');
        permissions.forEach(p => console.log(`  id=${p.id} type=${p.type} allow=${p.allow.toArray()}`));

        const visibleRoleIds = permissions.filter(
            perm =>
                perm.type === 0 &&
                perm.allow.has(PermissionFlagsBits.ViewChannel) &&
                !excludedRoleIds.includes(perm.id)
        );

        console.log('[list-team-drivers] Eligible role overwrites found:', visibleRoleIds.size);

        if (visibleRoleIds.size === 0) {
            console.log('[list-team-drivers] No eligible roles, aborting');
            return interaction.editReply('No eligible team role found in this channel. Contact @Joscha Maier');
        }

        const teamRoleId = visibleRoleIds.first().id;
        console.log('[list-team-drivers] Team role ID:', teamRoleId);

        const teamRole = await interaction.guild.roles.fetch(teamRoleId);
        console.log('[list-team-drivers] Resolved team role:', teamRole ? teamRole.name : 'NOT FOUND');

        if (!teamRole) {
            return interaction.editReply('Team role could not be resolved.');
        }

        console.log('[list-team-drivers] Fetching all members for role:', teamRole.name);

        const allMembers = await interaction.guild.members.fetch();
const membersWithRole = allMembers
  .filter(member => member.roles.cache.has(teamRoleId))
  .map(member => `${member.displayName} (@${member.user.username}) - ${member.id}`);

        console.log(`[list-team-drivers] Total members fetched: ${allMembers.size}`);

        //const membersWithRole = teamRole.members.map(m => m.toString());
        console.log('[list-team-drivers] Members with team role:', membersWithRole.length);

        const embed = new EmbedBuilder()
            .setTitle(`Members with ${teamRole.name}`)
            .setColor(0x2b2d31)
            .setDescription(
                membersWithRole.length > 0
                    ? membersWithRole.join('\n')
                    : 'No users currently have this role.'
            );

        console.log('[list-team-drivers] Sending embed reply');
        await interaction.editReply({ embeds: [embed] });
        console.log('[list-team-drivers] Done');
    }
};