const { SlashCommandBuilder, PermissionFlagsBits, ChannelType } = require('discord.js');

module.exports = {
  data: new SlashCommandBuilder()
    .setName('setup-teams')
    .setDescription('Creates roles and private channels for each team.')
    .addStringOption(option =>
      option.setName('teams')
        .setDescription('Comma-separated list of team names')
        .setRequired(true))
    .setDefaultMemberPermissions(PermissionFlagsBits.Administrator),

  async execute(interaction) {
    const guild = interaction.guild;
    const racespotRoleId = '318989503090130944';
    const raceOfficialsRoleId = '341286151920812033';
    const categoryId = '1391067294012280862';

    const teamInput = interaction.options.getString('teams');
    const teamNames = teamInput.split(',').map(name => name.trim()).filter(Boolean);

    for (const teamName of teamNames) {
      // Create role
      const role = await guild.roles.create({
        name: teamName,
        reason: 'Team role creation',
      });

      // Create channel under the category
      const channel = await guild.channels.create({
        name: teamName.toLowerCase().replace(/\s+/g, '-').replace(/[^a-z0-9\-]/g, ''),
        type: ChannelType.GuildText,
        parent: categoryId,
        permissionOverwrites: [
          {
            id: guild.roles.everyone.id,
            deny: [PermissionFlagsBits.ViewChannel],
          },
          {
            id: racespotRoleId,
            allow: [PermissionFlagsBits.ViewChannel],
          },
          {
            id: raceOfficialsRoleId,
            allow: [PermissionFlagsBits.ViewChannel],
          },
          {
            id: role.id,
            allow: [PermissionFlagsBits.ViewChannel],
          }
        ],
      });
    }

    await interaction.reply(`✅ Created ${teamNames.length} team roles and channels in the 2025 category.`);
  }
};
