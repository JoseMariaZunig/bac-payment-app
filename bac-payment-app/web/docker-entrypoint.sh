#!/bin/sh
set -e

API_URL="${API_BASE_URL:-http://localhost:8080}"

cat > /usr/share/nginx/html/config.js <<EOF
window.APP_CONFIG = {
  API_BASE_URL: "${API_URL}"
};
EOF

exec nginx -g "daemon off;"
