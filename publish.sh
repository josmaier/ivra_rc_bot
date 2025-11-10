#!/bin/bash
# Exit bei Fehler
set -e

# Ordnerpfad
PUBLISH_DIR="publish"

# Alten Publish-Ordner löschen, falls vorhanden
if [ -d "$PUBLISH_DIR" ]; then
    echo "Deleting existing publish folder..."
    rm -rf "$PUBLISH_DIR"
fi

# Anwendung publishen
echo "Publishing .NET app (Linux-x64, self-contained)..."
dotnet publish RaceControlBot.csproj -c Release -r linux-x64 --self-contained true -o "$PUBLISH_DIR"

echo "Publish completed successfully: $PUBLISH_DIR"
