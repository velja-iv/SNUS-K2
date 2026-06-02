@echo off
pushd "%~dp0"
echo Starting Cupid Client...
start "Cupid Client" cmd /k dotnet run --project "%~dp0Cupid.Client\Cupid.Client.csproj"
popd
