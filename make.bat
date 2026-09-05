@echo off
setlocal

set "ROOT=%~dp0"
set "OUT=%ROOT%publish"

echo === Publishing twm.exe ===
dotnet publish "%ROOT%src\Twm.App\Twm.App.csproj" -c Release -p:PublishProfile=win-x64-aot -o "%OUT%"
if errorlevel 1 (
    echo FAILED: twm.exe publish
    exit /b 1
)

echo === Publishing twm-msg.exe ===
dotnet publish "%ROOT%src\Twm.Msg\Twm.Msg.csproj" -c Release -p:PublishProfile=win-x64-aot -o "%OUT%"
if errorlevel 1 (
    echo FAILED: twm-msg.exe publish
    exit /b 1
)

echo.
echo === Done. Native binaries in "%OUT%" ===
dir /b "%OUT%\*.exe"
