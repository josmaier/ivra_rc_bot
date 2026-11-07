module.exports = {
  apps: [
    {
      name: "ivra-rc-bot-24h",
      script: "index.js",
      // Run from the bot folder so .env and sheet.json are found
      cwd: __dirname,
      watch: false,
      autorestart: true,
      max_restarts: 10,
      env: {
        NODE_ENV: "production"
      }
    }
  ]
};
