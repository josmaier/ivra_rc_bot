const dotenv = require('dotenv');
dotenv.config({path: "./vars/.env"});
// Require the necessary discord.js classes
const { Client, Events, GatewayIntentBits } = require('discord.js');

// Create a new client instance
const client = new Client({ intents: [GatewayIntentBits.Guilds] });

// When the client is ready, run this code (only once)
// We use 'c' for the event parameter to keep it separate from the already defined 'client'
client.once(Events.ClientReady, c => {
    const restartChannel = client.channels.find(item => item.name === process.env.RESTART_CHANNEL_NAME)
	restartChannel.send(`Started succesfully!
	\n Changelog:
	\n	1.0 Initial rewrite`);
    console.log(`Ready! Logged in as ${c.user.tag}`);
});

// Log in to Discord with your client's token
client.login(process.env.TOKEN);