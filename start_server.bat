@echo off
pushd "%~dp0"
echo Restoring and building solution...
dotnet restore
dotnet build

echo Starting Cupid Server...
start "Cupid Server" cmd /k dotnet run --project "%~dp0Cupid.Server\Cupid.Server.csproj"
popd
