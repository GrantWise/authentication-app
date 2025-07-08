#!/bin/bash
cd /home/grant/authentication/backend/AuthenticationApi
rm -rf Migrations
dotnet ef migrations add InitialCreate