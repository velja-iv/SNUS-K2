@echo off
pushd "%~dp0"
echo Restoring and building solution...
dotnet clean
dotnet build

echo Starting Cupid Server...
start "Cupid Server" cmd /k dotnet run --project "%~dp0Cupid.Server\Cupid.Server.csproj"
popd

if exist "Cupid.Models.obj" rmdir /s /q "Cupid.Models.obj"
if exist "Cupid.Server.obj" rmdir /s /q "Cupid.Server.obj"
if exist "Cupid.Client.obj" rmdir /s /q "Cupid.Client.obj"
