powershell -NoProfile -ExecutionPolicy Bypass -Command "& {Import-Module .\Tools\PSake\psake.psm1; Invoke-psake .\build.ps1 %*}"
pause