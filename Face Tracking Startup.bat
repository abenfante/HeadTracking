@echo off

setlocal EnableExtensions DisableDelayedExpansion

set /P xRes="Inserisci la risoluzione orizzontale della webcam: "
set /P yRes="Inserisci la risoluzione verticale della webcam: "
set /P cameraIndex="Inserisci l'id della webcam che vuoi utilizzare (nel dubbio provare con 0 e poi i numeri immediatamente successivi: "

start .\dist\project.exe %xRes% %yRes% %cameraIndex%
timeout /t 10
start .\Unity_build\HeadTracking.exe %xRes% %yRes%