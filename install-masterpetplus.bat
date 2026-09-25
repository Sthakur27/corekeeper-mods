@echo off
rem Copies the MasterPetPlus mod into Core Keeper's local mod folder (SideLoader).
rem Disable the mod.io "Master Pet" mod in-game; this fork replaces it.
set SRC=%~dp0MasterPetPlus
set DST=C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\Mods\MasterPetPlus
if exist "%DST%" rmdir /s /q "%DST%"
xcopy /e /i /y "%SRC%" "%DST%" >nul
copy /b "%DST%\ModManifest.json"+,, "%DST%\ModManifest.json" >nul
echo Installed to "%DST%"
