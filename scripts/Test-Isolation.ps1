param([string]$Executable=(Join-Path $PSScriptRoot '../src/Zetmar.Battery/bin/Release/net10.0-windows/ZETMAR Battery.exe'))
$ErrorActionPreference='Stop'
$profile=Join-Path $env:LOCALAPPDATA 'ZetmarBattery/settings.json'
$before=if(Test-Path $profile){(Get-FileHash -LiteralPath $profile).Hash}else{'missing'}
$idsBefore=@(Get-Process 'ZETMAR Battery' -ErrorAction SilentlyContinue | ForEach-Object {$_.Id})
$ErrorActionPreference='Continue'
powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Smoke-Test.ps1') -Executable $Executable -FailAfterStart 2>&1 | Out-File (Join-Path $PSScriptRoot '../artifacts/injected-failure.txt')
$code=$LASTEXITCODE
$ErrorActionPreference='Stop'
if($code -eq 0){throw 'Failure injection did not fail'}
$after=if(Test-Path $profile){(Get-FileHash -LiteralPath $profile).Hash}else{'missing'}
if($before -ne $after){throw 'User settings changed'}
$new=@(Get-Process 'ZETMAR Battery' -ErrorAction SilentlyContinue | Where-Object {$_.Id -notin $idsBefore})
if($new.Count -gt 0){throw 'Test left child processes running'}
'PASS: injected GUI test failure cleans up child process and preserves user settings'
