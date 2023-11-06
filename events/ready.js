const { Events } = require('discord.js');

module.exports = {
	name: Events.ClientReady,
	once: true,
	execute(client) {
		console.log(`Ready! Logged in as ${client.user.tag}`);
        client.channels.fetch(process.env.RESTART_CHANNEL_ID)
		.then(channel => channel.send(`Started succesfully!
		\n Changelog:
		\n	1.0 First Deployment
		\n  1.1 Fixed Protest command, elaborated error messages, added info command
		\n  1.2 Added embeds to /team and /rc to display the send message
		\n  1.3 Added Ability to ping roles to /team`))
		.catch(console.error);
	},
};