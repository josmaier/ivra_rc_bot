const { SlashCommandBuilder, PermissionFlagsBits } = require('discord.js');

module.exports = {
  data: new SlashCommandBuilder()
    .setName('remove-driver')
    .setDescription('Removes the driver and team role from specified users based on the current channel.')
    .addStringOption(option =>
      option.setName('user_ids')
        .setDescription('Comma-separated list of user IDs to remove roles from')
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
      return interaction.reply({ content: 'You are not authorized to use this command.', ephemeral: true });
    }

    await interaction.deferReply();

    const userIds = interaction.options.getString('user_ids')
      .split(',')
      .map(id => id.trim())
      .filter(Boolean);

    const channel = interaction.channel;

    // Find valid team role (visible in this channel, and not one of the excluded roles)
    const permissions = channel.permissionOverwrites.cache;
    const visibleRoleIds = permissions
      .filter(perm => perm.type === 0 && perm.allow.has('ViewChannel') && !excludedRoleIds.includes(perm.id))
      .map(perm => perm.id);

    if (visibleRoleIds.size === 0) {
      return interaction.editReply(' No eligible team role found in this channel. Contact @Joscha Maier');
    }

    const teamRoleId = visibleRoleIds[0];
    const teamRole = interaction.guild.roles.cache.get(teamRoleId);

    if (!teamRole) {
      return interaction.editReply('Team role could not be resolved.');
    }

    const results = { success: [], failed: [] };

    for (const userId of userIds) {
      try {
        const user = await interaction.guild.members.fetch(userId);
        await user.roles.remove([teamRoleId, driverRoleId]);
        results.success.push(userId);
      } catch (err) {
        console.warn(`Failed to remove roles from ${userId}:`, err.message);
        results.failed.push(userId);
      }
    }

    let reply = `✅ Removed **${teamRole.name}** and **Driver** role from: ${results.success.join(', ')}`;
    if (results.failed.length > 0) {
      reply += `\n⚠️ Failed for: ${results.failed.join(', ')}`;
    }

    await interaction.editReply(reply);
  }
};
