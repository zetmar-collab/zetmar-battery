param([ValidateSet('win-x64','win-arm64')][string]$Runtime='win-x64',[string]$RuntimeVersion='10.0.14')
$ErrorActionPreference='Stop'
Push-Location (Join-Path $PSScriptRoot '..')
try {
    python scripts/Extract-Translations.py
    if($LASTEXITCODE -ne 0){throw 'Translations invalid'}
    dotnet run --project tests/Zetmar.Tests -c Release
    if($LASTEXITCODE -ne 0){throw 'Core tests failed'}
    dotnet run --project tests/Zetmar.WindowsTests -c Release
    if($LASTEXITCODE -ne 0){throw 'Windows tests failed'}
    $stage=Join-Path 'dist/releases' ($Runtime+'-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,6))
    $output="dist/ZETMAR-Battery-$Runtime"
    dotnet publish src/Zetmar.Battery -c Release -r $Runtime --self-contained true "-p:RuntimeFrameworkVersion=$RuntimeVersion" -o $stage
    if($LASTEXITCODE -ne 0){throw 'Publish failed'}
    Copy-Item README.md,LICENSE,CHANGELOG.md,VERIFICATION.md,WDROZENIE_AUDYTU.md,UI_UX_1.3.md -Destination $stage
    $cache=if($env:NUGET_PACKAGES){$env:NUGET_PACKAGES}else{Join-Path $env:USERPROFILE '.nuget/packages'}
    $core=Join-Path $cache "microsoft.netcore.app.runtime.$Runtime/$RuntimeVersion"
    Copy-Item (Join-Path $core 'LICENSE.TXT') (Join-Path $stage 'DOTNET-LICENSE.txt')
    Copy-Item (Join-Path $core 'THIRD-PARTY-NOTICES.TXT') (Join-Path $stage 'DOTNET-THIRD-PARTY-NOTICES.txt')
    $desktop=Join-Path $cache "microsoft.windowsdesktop.app.runtime.$Runtime/$RuntimeVersion"
    if(Test-Path (Join-Path $desktop 'LICENSE')){Copy-Item (Join-Path $desktop 'LICENSE') (Join-Path $stage 'WINDOWSDESKTOP-LICENSE.txt')}
    if(Test-Path (Join-Path $desktop 'THIRD-PARTY-NOTICES.TXT')){Copy-Item (Join-Path $desktop 'THIRD-PARTY-NOTICES.TXT') (Join-Path $stage 'WINDOWSDESKTOP-THIRD-PARTY-NOTICES.txt')}
    New-Item -ItemType Directory -Force -Path $output | Out-Null
    Copy-Item -Path "$stage/*" -Destination $output -Force
    $archive="dist/ZETMAR-Battery-$Runtime-portable.zip"
    Compress-Archive -Path "$stage/*" -DestinationPath $archive -Force
    $lines=@()
    $lines+=(Get-FileHash "$stage/ZETMAR Battery.exe" -Algorithm SHA256).Hash+"  ZETMAR-Battery-$Runtime/ZETMAR Battery.exe"
    $lines+=(Get-FileHash $archive -Algorithm SHA256).Hash+"  ZETMAR-Battery-$Runtime-portable.zip"
    $lines | Set-Content "dist/SHA256SUMS-$Runtime.txt"
    Get-ChildItem dist -Filter 'SHA256SUMS-win-*.txt' | Sort-Object Name | ForEach-Object {Get-Content -LiteralPath $_.FullName} | Set-Content dist/SHA256SUMS.txt
    Write-Output "Published from clean staging: $stage"
} finally {Pop-Location}
