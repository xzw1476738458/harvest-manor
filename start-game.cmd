@echo off
setlocal

set "ROOT_DIR=%~dp0"
set "GAME_DIR=%ROOT_DIR%game"
set "PORTABLE_GODOT_DIR=%ROOT_DIR%tools\godot"
set "DEFAULT_GODOT_CONSOLE_EXE=C:\Program Files (x86)\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64_console.exe"

REM Resolve Godot in this priority order:
REM   1) GODOT_CONSOLE_EXE env var (explicit override)
REM   2) Portable copy under tools\godot\ (recursive search for *_console.exe)
REM   3) Default system install path
if defined GODOT_CONSOLE_EXE goto resolve_done

if exist "%PORTABLE_GODOT_DIR%" (
    for /r "%PORTABLE_GODOT_DIR%" %%F in (*_console.exe) do (
        set "GODOT_CONSOLE_EXE=%%F"
        goto resolve_done
    )
)

set "GODOT_CONSOLE_EXE=%DEFAULT_GODOT_CONSOLE_EXE%"

:resolve_done

if exist "%GODOT_CONSOLE_EXE%" goto godot_found
echo Godot console executable was not found:
echo   "%GODOT_CONSOLE_EXE%"
echo.
echo Drop a portable Godot Mono build under:
echo   "%PORTABLE_GODOT_DIR%"
echo or set GODOT_CONSOLE_EXE, or edit this script to match your Godot install path.
pause
exit /b 1
:godot_found

if exist "%GAME_DIR%\project.godot" goto project_found
echo Godot project file was not found:
echo   "%GAME_DIR%\project.godot"
pause
exit /b 1
:project_found

pushd "%GAME_DIR%" >nul
"%GODOT_CONSOLE_EXE%" --path "%GAME_DIR%" %*
set "EXIT_CODE=%ERRORLEVEL%"
popd >nul

if not "%EXIT_CODE%"=="0" (
    echo.
    echo Godot exited with code %EXIT_CODE%.
    pause
)

exit /b %EXIT_CODE%
