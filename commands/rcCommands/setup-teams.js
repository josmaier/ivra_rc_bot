const { SlashCommandBuilder, PermissionFlagsBits, ChannelType } = require('discord.js');

module.exports = {
  data: new SlashCommandBuilder()
    .setName('setup-teams')
    .setDescription('Creates roles and private channels; skips existing ones and handles overflows.')
    .addStringOption(option =>
      option.setName('teams')
        .setDescription('Comma-separated list of team names')
        .setRequired(true))
    .addStringOption(option =>
      option.setName('categories')
        .setDescription('Comma-separated list of Category IDs')
        .setRequired(true))
    .setDefaultMemberPermissions(PermissionFlagsBits.Administrator),

  async execute(interaction) {
    await interaction.deferReply();

    const guild = interaction.guild;
    const racespotRoleId = '318989503090130944';
    const raceOfficialsRoleId = '341286151920812033';

    const teamInput = interaction.options.getString('teams');
    const categoryInput = interaction.options.getString('categories');

    const teamNames = teamInput.split(',').map(name => name.trim()).filter(Boolean);
    const categoryIds = categoryInput.split(',').map(id => id.trim()).filter(Boolean);

    const MAX_CHANNELS_PER_CAT = 48;
    let currentCatIndex = 0;

    console.log(`\n--- Starting Team Setup for ${teamNames.length} teams ---`);
    console.log(`Target Category IDs: ${categoryIds.join(', ')}`);

    // 1. Full API Fetches
    console.log('Fetching live roles and channels from Discord API...');
    const allRoles = await guild.roles.fetch();
    const allChannels = await guild.channels.fetch();

    let createdCount = 0;
    let skippedCount = 0;

    for (const teamName of teamNames) {
      const sanitizedName = teamName.toLowerCase().replace(/\s+/g, '-').replace(/[^a-z0-9\-]/g, '');

      // 2. Check for existing channel in target categories
      const existingChannel = allChannels.find(channel => 
        categoryIds.includes(channel.parentId) && 
        channel.name === sanitizedName
      );

      if (existingChannel) {
        console.log(`[SKIP] Channel "${sanitizedName}" already exists in category ${existingChannel.parentId}.`);
        skippedCount++;
        continue;
      }

      // 3. Role Logic
      let role = allRoles.find(r => r.name.toLowerCase() === teamName.toLowerCase());
      if (role) {
        console.log(`[INFO] Reusing existing role: ${role.name} (${role.id})`);
      } else {
        role = await guild.roles.create({
          name: teamName,
          reason: 'Team setup: New role created',
        });
        console.log(`[NEW] Created role: ${role.name}`);
      }

      // 4. Category Management & Overflow
      let targetCategoryId = categoryIds[currentCatIndex];
      let currentChildrenCount = allChannels.filter(c => c.parentId === targetCategoryId).size;

      if (currentChildrenCount >= MAX_CHANNELS_PER_CAT) {
        if (currentCatIndex < categoryIds.length - 1) {
          const oldCatId = targetCategoryId;
          currentCatIndex++;
          targetCategoryId = categoryIds[currentCatIndex];
          console.log(`[OVERFLOW] Category ${oldCatId} is full. Moving to next: ${targetCategoryId}`);
        } else {
          console.error(`[CRITICAL] All provided categories are full! Stopping at ${teamName}.`);
          break;
        }
      }

      // 5. Channel Creation
      const newChannel = await guild.channels.create({
        name: sanitizedName,
        type: ChannelType.GuildText,
        parent: targetCategoryId,
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

      // Update our local "allChannels" collection so the next loop sees the new channel
      allChannels.set(newChannel.id, newChannel);
      
      createdCount++;
      console.log(`[SUCCESS] Created channel #${sanitizedName} in category ${targetCategoryId}`);
    }

    console.log(`--- Setup Finished. Created: ${createdCount}, Skipped: ${skippedCount} ---\n`);
    await interaction.editReply(
      `✅ **Process Complete**\n- Channels Created: ${createdCount}\n- Teams Skipped: ${skippedCount}`
    );
  }
};