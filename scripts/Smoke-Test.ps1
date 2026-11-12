param([string]$Executable = (Join-Path $PSScriptRoot '..\src\Zetmar.Battery\bin\Release\net10.0-windows\ZETMAR Battery.exe'),[switch]$FailAfterStart)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes,System.Drawing,System.Windows.Forms
. (Join-Path $PSScriptRoot 'Test-Support.ps1')
Initialize-TestProfile
$process = $null
$second = $null
try {
$process = Start-Process -FilePath $Executable -ArgumentList $TestArguments -PassThru -WindowStyle Hidden
Wait-TestCondition {
    $process.Refresh(); if($process.HasExited) { throw 'Application exited before showing panel' }
    [System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.AndCondition]::new([System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'ZETMAR Battery'),[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$process.Id)))
} 'initial panel' | Out-Null
if($FailAfterStart){throw 'Injected test failure'}
$process.Refresh()
if ($process.HasExited) { throw "Application exited: $($process.ExitCode)" }
$root = [System.Windows.Automation.AutomationElement]::RootElement
$condition = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$process.Id)
$windows = $root.FindAll([System.Windows.Automation.TreeScope]::Children,$condition)
$labels = @()
foreach ($window in $windows) {
    $labels += 'WINDOW: ' + $window.Current.Name
    $all = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)
    foreach ($item in $all) { if ($item.Current.Name) { $labels += $item.Current.Name } }
}
$labels | Set-Content -Encoding utf8 (Join-Path $PSScriptRoot '..\artifacts\ui-labels.txt')
if ($labels -notcontains 'ZETMAR Battery') { throw 'Details panel not found' }
$panel = $windows | Where-Object { $_.Current.Name -eq 'ZETMAR Battery' } | Select-Object -First 1
$rect = $panel.Current.BoundingRectangle
$bmp = [System.Drawing.Bitmap]::new([int]$rect.Width,[int]$rect.Height)
$graphics = [System.Drawing.Graphics]::FromImage($bmp)
Add-Type -TypeDefinition 'using System; using System.Runtime.InteropServices; public static class WindowCapture { [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr dc, uint flags); }'
$dc = $graphics.GetHdc()
try { $captured = [WindowCapture]::PrintWindow([IntPtr]$panel.Current.NativeWindowHandle,$dc,2); if (-not $captured) { throw 'Window capture failed' } } finally { $graphics.ReleaseHdc($dc) }
$bmp.Save((Join-Path $PSScriptRoot '..\artifacts\panel.png'))
$graphics.Dispose(); $bmp.Dispose()
$panel.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()
Start-Sleep -Milliseconds 300
$second = Start-Process -FilePath $Executable -ArgumentList $TestArguments -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 2
$second.Refresh()
if (-not $second.HasExited) { throw 'Second instance did not exit' }
$windows = $root.FindAll([System.Windows.Automation.TreeScope]::Children,$condition)
$panel = $windows | Where-Object { $_.Current.Name -eq 'ZETMAR Battery' } | Select-Object -First 1
if (-not $panel) { throw 'Second launch did not reopen existing panel' }
$process.Refresh()
$cpuStart = $process.TotalProcessorTime
Start-Sleep -Seconds 10
$process.Refresh()
$cpu = ($process.TotalProcessorTime - $cpuStart).TotalSeconds
"PID: $($process.Id); private MB: $([math]::Round($process.PrivateMemorySize64/1MB,1)); working set MB: $([math]::Round($process.WorkingSet64/1MB,1)); CPU seconds in 10s: $cpu" | Set-Content (Join-Path $PSScriptRoot '..\artifacts\performance.txt')
$exitCondition = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'Zakończ aplikację')
$exit = $panel.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$exitCondition)
if (-not $exit) {
    $exitCondition = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'Exit application')
    $exit = $panel.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$exitCondition)
}
if (-not $exit) { throw 'Exit button not found' }
$pattern = $exit.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
$pattern.Invoke()
if (-not $process.WaitForExit(5000)) { throw 'Exit did not stop process' }
'PASS: panel, real labels, single instance, clean exit'
Get-Content (Join-Path $PSScriptRoot '..\artifacts\performance.txt')

} finally {
    try { Stop-TestProcess $second } finally { Stop-TestProcess $process }
}
