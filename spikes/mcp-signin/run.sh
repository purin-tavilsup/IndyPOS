#!/usr/bin/env bash
# Starts the spike behind the current quick tunnel. Usage: ./run.sh https://<host>.trycloudflare.com
cd "$(dirname "$0")"
Spike__PublicUrl="$1" Spike__AllowedEmails=purin.tavilsup@gmail.com \
ASPNETCORE_URLS=http://localhost:5199 ASPNETCORE_ENVIRONMENT=Development \
dotnet run --no-build --no-launch-profile
