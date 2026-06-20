#!/usr/bin/env bash
cd "$(dirname "$0")"
[ -d node_modules ] || npm install
PORT=${PORT:-8095} npm start
