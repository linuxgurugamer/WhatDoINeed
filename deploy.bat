
@echo off

rem H is the destination game folder
rem GAMEDIR is the name of the mod folder (usually the mod name)
rem GAMEDATA is the name of the local GameData
rem VERSIONFILE is the name of the version file, usually the same as GAMEDATA,
rem    but not always

set H=%KSPDIR%

set H1=R:\KSP_1.12.5-CenturianMaximus
set H2=R:\KSP_1.12.5_Career-MissionController-JNSQ
set H3=R:\KSP_1.12.5_Candy-Career

set H4=R:\KSP_1.12.5-WhatDoINeed


set GAMEDIR=WhatDoINeed
set GAMEDATA="GameData"
set VERSIONFILE=%GAMEDIR%.version

set DP0=r:\dp0\kspdev

copy /Y "%1%2" "%GAMEDATA%\%GAMEDIR%\Plugins"
copy /Y "%1%3".pdb "%GAMEDATA%\%GAMEDIR%\Plugins"
copy /Y changelog.cfg %GAMEDATA%\%GAMEDIR%

copy /Y %VERSIONFILE% %GAMEDATA%\%GAMEDIR%

echo %H%
xcopy /y /s /I %GAMEDATA%\%GAMEDIR% "%H%\GameData\%GAMEDIR%"
echo %H1%
xcopy /y /s /I %GAMEDATA%\%GAMEDIR% "%H1%\GameData\%GAMEDIR%"
echo %H2%
xcopy /y /s /I %GAMEDATA%\%GAMEDIR% "%H2%\GameData\%GAMEDIR%"
echo %H3%
xcopy /y /s /I %GAMEDATA%\%GAMEDIR% "%H3%\GameData\%GAMEDIR%"
echo %H4%
xcopy /y /s /I %GAMEDATA%\%GAMEDIR% "%H4%\GameData\%GAMEDIR%"

rem pause
