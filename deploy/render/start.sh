#!/bin/sh
set -eu

: "${DATABASE_URL:?DATABASE_URL is required}"
: "${PORT:?PORT is required}"
: "${RENDER_EXTERNAL_HOSTNAME:?RENDER_EXTERNAL_HOSTNAME is required}"

if [ "${SWITCHKOREA_HOSTING_MODE:-}" != "Render" ]; then
    echo "SWITCHKOREA_HOSTING_MODE must be Render." >&2
    exit 1
fi

dotnet /setup/SalesManagement.Setup.dll --migrate-only
exec dotnet /app/SalesManagement.Api.dll --urls "http://0.0.0.0:${PORT}"
