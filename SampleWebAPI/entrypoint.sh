#!/bin/bash
set -e

echo "Waiting for SQL Server to be ready..."
# Wait for SQL Server health check
until /opt/mssql-tools/bin/sqlcmd -S sqlserver -U sa -P "${DB_PASSWORD}" -Q "SELECT 1" &> /dev/null
do
  echo "SQL Server is unavailable - sleeping"
  sleep 5
done

echo "SQL Server is up - running migrations..."
dotnet ef database update --no-build

echo "Starting application..."
exec dotnet SampleWebAPI.dll