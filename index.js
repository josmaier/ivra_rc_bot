require('dotenv').config();
// Require the necessary discord.js classes
const { Client, GatewayIntentBits, Collection, Events } = require('discord.js');
const { channel } = require('node:diagnostics_channel');
//adding the requirement for the node file system and path
const fs = require('node:fs');
const path = require('node:path');


// Create a new client instance
const client = new Client({ intents: [GatewayIntentBits.Guilds] });

//Telling program that we want to create a collection with all commands
client.commands = new Collection();
//adding the commands to the collection
const foldersPath  = path.join(__dirname, 'commands'); //creating path to folder
const commandFolders = fs.readdirSync(foldersPath);

for (const folder of commandFolders) {
	const commandsPath = path.join(foldersPath, folder);
	const commandFiles = fs.readdirSync(commandsPath).filter(file => file.endsWith('.js'));
	for (const file of commandFiles) {
		const filePath = path.join(commandsPath, file);
		const command = require(filePath);
		// Set a new item in the Collection with the key as the command name and the value as the exported module
		if ('data' in command && 'execute' in command) {
			client.commands.set(command.data.name, command);
		} else {
			console.log(`[WARNING] The command at ${filePath} is missing a required "data" or "execute" property.`);
		}
	}
}



//-------------------------------------------Finished setup------------------------------------------------------------


// Log in to Discord with your client's token
client.login(process.env.TOKEN);

//if a user uses a command excecute that commands function if it exists
client.on(Events.InteractionCreate, async interaction => {
	if (!interaction.isChatInputCommand()) return;

	const command = interaction.client.commands.get(interaction.commandName);

	if (!command) {
		console.error(`No command matching ${interaction.commandName} was found.`);
		return;
	}

	try {
		await command.execute(interaction);
	} catch (error) {
		console.error(error);
		if (interaction.replied || interaction.deferred) {
			await interaction.followUp({ content: 'There was an error while executing this command! \n Please try again or contact RC if the issue persists', ephemeral: true });
		} else {
			await interaction.reply({ content: 'There was an error while executing this command!\n Please try again or contact RC if the issue persists', ephemeral: true });
		}
	}
});

client.once(Events.ClientReady, c => {
	console.log(`Ready! Logged in as ${c.user.tag}`);

	client.channels.fetch(process.env.RESTART_CHANNEL_ID)
		.then(channel => channel.send(`Started succesfully!
		\n Changelog:
		\n	1.0 First Deployment`))
		.catch(console.error);
});