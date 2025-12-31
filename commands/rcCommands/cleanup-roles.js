const { SlashCommandBuilder, PermissionFlagsBits } = require('discord.js');

module.exports = {
  data: new SlashCommandBuilder()
    .setName('cleanup-roles')
    .setDescription('Removes numbered team roles and the Driver role from all users.')
    .setDefaultMemberPermissions(PermissionFlagsBits.Administrator),

  async execute(interaction) {
    const guild = interaction.guild;
    const driverRoleId = '1391075620783525988';
    const removedRoles = [];
    const regex = /^\d+-/;

    await interaction.deferReply({ ephemeral: true });

    // 1. Remove numbered roles from users and delete them
    for (const role of guild.roles.cache.values()) {
      if (regex.test(role.name)) {
        try {
          const membersWithRole = await guild.members.fetch({ force: true });
          for (const member of membersWithRole.values()) {
            if (member.roles.cache.has(role.id)) {
              await member.roles.remove(role);
            }
          }
          await role.delete('Cleaned up numbered team roles');
          removedRoles.push(role.name);
        } catch (err) {
          console.warn(`Failed to delete role ${role.name}:`, err.message);
        }
      }
    }

    // 2. Remove Driver role from all users
    try {
      const membersWithDriver = await guild.members.fetch({ force: true });
      let driverRemovals = 0;
      for (const member of membersWithDriver.values()) {
        if (member.roles.cache.has(driverRoleId)) {
          await member.roles.remove(driverRoleId);
          driverRemovals++;
        }
      }

      await interaction.editReply(`✅ Removed and deleted ${removedRoles.length} numbered roles: ${removedRoles.join(', ')}\n✅ Removed "Driver" role from ${driverRemovals} members.`);
    } catch (err) {
      await interaction.editReply('❌ Error occurred during cleanup.');
      console.error(err);
    }
  }
};
