@echo off
rem Copies a mod from this folder into Core Keeper's local mod folder.
rem Usage: install.bat [ModName]   (default: LoadoutSharing)
rem The game's side-loader picks up the folder at the next launch.
set MOD=%~1
if "%MOD%"=="" set MOD=LoadoutSharing
set SRC=%~dp0%MOD%
set DST=C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\Mods\%MOD%
if not exist "%SRC%\ModManifest.json" (
    echo No mod named "%MOD%" in %~dp0
    exit /b 1
)
if exist "%DST%" rmdir /s /q "%DST%"
xcopy /e /i /y "%SRC%" "%DST%" >nul
copy /b "%DST%\ModManifest.json"+,, "%DST%\ModManifest.json" >nul
echo Installed %MOD% to "%DST%"
