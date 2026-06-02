@echo off
pushd "%~dp0"
if "%1"=="" (
  set CLIENTS=3
) else (
  set CLIENTS=%1
)

echo Restoring and building solution...
dotnet restore
dotnet build

echo Starting Cupid Server...
start "Cupid Server" cmd /k dotnet run --project "%~dp0Cupid.Server\Cupid.Server.csproj"

timeout /t 2 >nul

for /L %%i in (1,1,%CLIENTS%) do (
  start "Cupid Client %%i" cmd /k dotnet run --project "%~dp0Cupid.Client\Cupid.Client.csproj"
  timeout /t 1 >nul
)

popd
