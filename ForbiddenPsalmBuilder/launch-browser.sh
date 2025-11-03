#!/bin/bash
# ChromeOS browser launcher script for Blazor development

URL=${1:-"http://penguin.linux.test:5265"}

echo "Opening $URL in ChromeOS browser..."

# Try different methods to open browser on ChromeOS
if command -v xdg-open >/dev/null 2>&1; then
    xdg-open "$URL"
elif [ -n "$BROWSER" ]; then
    "$BROWSER" "$URL"
elif command -v garcon-url-handler >/dev/null 2>&1; then
    garcon-url-handler "$URL"
else
    echo "Please manually open: $URL"
    echo "Copy this URL and paste it into your Chrome browser"
fi