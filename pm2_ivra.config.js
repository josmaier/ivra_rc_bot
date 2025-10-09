module.exports = {
  apps: [
    {
      name: "IVRA RC Bot",
      script: "iRacingMaintenanceWatcher.js",
      watch: false,
      autorestart: true,
      max_restarts: 10,
      env: {
        NODE_ENV: "production"
      }
      // If you prefer envs here instead of .env, put them under env: { … }.
    }
  ]
};