@echo off
pushd "%~dp0"
echo Starting 4 Cupid Clients...
start "Cupid Client Sara" cmd /k dotnet run --project "%~dp0Cupid.Client\Cupid.Client.csproj"
timeout /t 1 >nul
start "Cupid Client Marija" cmd /k dotnet run --project "%~dp0Cupid.Client\Cupid.Client.csproj"
timeout /t 1 >nul
start "Cupid Client Marko" cmd /k dotnet run --project "%~dp0Cupid.Client\Cupid.Client.csproj"
timeout /t 1 >nul
start "Cupid Client Nikola" cmd /k dotnet run --project "%~dp0Cupid.Client\Cupid.Client.csproj"
popd
