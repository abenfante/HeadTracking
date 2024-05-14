@echo off

setlocal EnableExtensions DisableDelayedExpansion

set /P xRes="Inserisci la risoluzione orizzontale della webcam:"
set /P yRes="Inserisci la risoluzione verticale della webcam:"

start .\dist\project.exe %xRes% %yRes%
timeout /t 10
start .\Unity_build\HeadTracking.exe %xRes% %yRes%