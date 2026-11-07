const { SlashCommandBuilder, PermissionFlagsBits } = require('discord.js');

module.exports = {
  data: new SlashCommandBuilder()
    .setName('assign-driver')
    .setDescription('Assigns the driver and team role based on the channel.')
    .addStringOption(option =>
      option.setName('user_ids')
        .setDescription('Comma-separated list of user IDs')
        .setRequired(true))
    .setDefaultMemberPermissions(PermissionFlagsBits.SendMessages),

  async execute(interaction) {
    const managerRoleId = '1131530691159531550';
    const driverRoleId = '1391075620783525988';
    const excludedRoleIds = [
      '307229025552564235',
      '341286151920812033',
      '318989503090130944',
      '343396972033474560'
    ];

    const member = await interaction.guild.members.fetch(interaction.user.id);
    if (!member.roles.cache.has(managerRoleId)) {
      return interaction.reply({ content: 'You are not authorized to use this command. This command is only available to team managers', ephemeral: true });
    }

    await interaction.deferReply();

    const userIds = interaction.options.getString('user_ids')
      .split(',')
      .map(id => id.trim())
      .filter(Boolean);

    const channel = interaction.channel;

    // Get roles that have permission to view the current channel
    const permissions = channel.permissionOverwrites.cache;
    const visibleRoleIds = permissions
      .filter(perm => perm.type === 0 && perm.allow.has('ViewChannel') && !excludedRoleIds.includes(perm.id))
      .map(perm => perm.id);

    if (visibleRoleIds.size === 0) {
      return interaction.editReply('No eligible team role found in this channel. Contact @Joscha Maier');
    }

    const teamRoleId = visibleRoleIds[0]; // assume one valid team role per channel
    const teamRole = interaction.guild.roles.cache.get(teamRoleId);

    if (!teamRole) {
      return interaction.editReply('Team role could not be resolved.');
    }

    const results = { success: [], failed: [] };

    for (const userId of userIds) {
      try {
        const user = await interaction.guild.members.fetch(userId);
        await user.roles.add([teamRoleId, driverRoleId]);
        results.success.push(userId);
      } catch (err) {
        console.warn(`Failed to assign roles to ${userId}:`, err.message);
        results.failed.push(userId);
      }
    }

    let reply = `✅ Assigned **${teamRole.name}** and **Driver** role to: ${results.success.join(', ')}`;
    if (results.failed.length > 0) {
      reply += `\nFailed for: ${results.failed.join(', ')}`;
    }

    await interaction.editReply(reply);
  }
};
