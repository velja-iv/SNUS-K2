@echo off
pushd "%~dp0"
echo Starting 4 Cupid Clients...
start "Cupid Client 1" cmd /k dotnet run --project "%~dp0Cupid.Client\Cupid.Client.csproj"
timeout /t 1 >nul
start "Cupid Client 2" cmd /k dotnet run --project "%~dp0Cupid.Client\Cupid.Client.csproj"
timeout /t 1 >nul
start "Cupid Client 3" cmd /k dotnet run --project "%~dp0Cupid.Client\Cupid.Client.csproj"
timeout /t 1 >nul
start "Cupid Client 4" cmd /k dotnet run --project "%~dp0Cupid.Client\Cupid.Client.csproj"
popd
