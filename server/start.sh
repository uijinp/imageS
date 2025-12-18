#!/bin/bash
# server_linux 실행 (로그는 server.log에 저장)
nohup ./server_linux > server.log 2>&1 &
echo "Server started. PID: $!"
