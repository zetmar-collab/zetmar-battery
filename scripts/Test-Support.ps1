# Common test support: never read or write the user's application profile.
function Initialize-TestProfile {
    $script:TestOutput = Join-Path $PSScriptRoot '../artifacts'
    New-Item -ItemType Directory -Force -Path $script:TestOutput | Out-Null
    $script:TestProfile = Join-Path $script:TestOutput ('test-runs/' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $script:TestProfile | Out-Null
    $script:TestArguments = @('--details','--data-dir',('"' + $script:TestProfile + '"'))
    [System.IO.File]::WriteAllText((Join-Path $script:TestProfile 'settings.json'),'{"Language":"en","WidgetVisible":true,"Alarm":false}')
}
function Wait-TestCondition([scriptblock]$Condition,[string]$Description,[int]$Seconds=20) {
    $end = [DateTime]::UtcNow.AddSeconds($Seconds)
    do {
        $result = & $Condition
        if ($result) { return $result }
        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $end)
    throw "Timed out: $Description"
}
function Stop-TestProcess($Target) {
    if (-not $Target) { return }
    $Target.Refresh()
    if ($Target.HasExited) { return }
    # Only the exact child process launched by this test can be stopped.
    if ($Target.Path -ne [System.IO.Path]::GetFullPath($Executable)) { throw 'Refusing to stop an unrelated process' }
    try {
        $condition = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$Target.Id)
        $windows = [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,$condition)
        foreach ($window in $windows) {
            $button = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'Exit application'))
            if ($button) { $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke(); break }
        }
    } finally {
        if (-not $Target.WaitForExit(3000)) { Stop-Process -Id $Target.Id -ErrorAction Stop }
    }
}
