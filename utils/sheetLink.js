const fs = require('node:fs');
const path = require('node:path');

// The link set with /set-sheet is stored here so it survives restarts
const storePath = path.join(__dirname, '..', 'sheet.json');

function getSheetUrl() {
    try {
        const data = JSON.parse(fs.readFileSync(storePath, 'utf8'));
        if (data.url) return data.url;
    } catch (error) {
        if (error.code !== 'ENOENT') console.error('Could not read sheet.json:', error);
    }
    // Fall back to the link from the .env file
    return process.env.SHEET_URL;
}

function setSheetUrl(url) {
    fs.writeFileSync(storePath, JSON.stringify({ url }, null, 2));
}

module.exports = { getSheetUrl, setSheetUrl };
