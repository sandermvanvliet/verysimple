#!/bin/sh

dotnet publish

rm -rf ~/Projects/Coolblue/session-state-poc/published-app/*

cp -r bin/Debug/netcoreapp1.0/publish/* ~/Projects/Coolblue/session-state-poc/published-app
