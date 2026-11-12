param([string]$Executable = (Join-Path $PSScriptRoot '..\src\Zetmar.Battery\bin\Release\net10.0-windows\ZETMAR Battery.exe'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes,System.Drawing
Add-Type -TypeDefinition 'using System; using System.Runtime.InteropServices; public static class ColorWindowCapture { [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr dc, uint flags); }'
. (Join-Path $PSScriptRoot 'Test-Support.ps1')
Initialize-TestProfile
$file = Join-Path $TestProfile 'settings.json'
$process = $null
function Find-Panel {
    $condition = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$process.Id)
    $windows = [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,$condition)
    return $windows | Where-Object { $_.Current.Name -eq 'ZETMAR Battery' } | Select-Object -First 1
}
function Find-Control($name) {
    $element = (Find-Panel).FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,$name))
    if (-not $element) { throw "Control not found: $name" }
    return $element
}
function Invoke-Control($name) { (Find-Control $name).GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke(); Start-Sleep -Milliseconds 200 }
function Set-Value($name,$value) { (Find-Control $name).GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($value) }
function Exit-App {
    if ($process -and -not $process.HasExited) { Invoke-Control 'Exit application'; if (-not $process.WaitForExit(5000)) { throw 'App did not exit' } }
}
try {
    $process = Start-Process -FilePath $Executable -ArgumentList $TestArguments -PassThru -WindowStyle Hidden
    Wait-TestCondition { Find-Panel } 'details panel' | Out-Null
    $nav=(Find-Panel).FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'Nav1'))
    $nav.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    Invoke-Control 'Widget background color — choose color'
    Start-Sleep -Milliseconds 500
    $dialogs = [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$process.Id))
    $dialog = $null
    foreach ($candidate in $dialogs) {
        if ($candidate.Current.ClassName -eq '#32770') { $dialog = $candidate; break }
        $nested = $candidate.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ClassNameProperty,'#32770'))
        if ($nested) { $dialog = $nested; break }
    }
    if (-not $dialog) { $dialogs | ForEach-Object { $_.Current.Name + ' / ' + $_.Current.ClassName } | Set-Content (Join-Path $PSScriptRoot '..\artifacts\picker-debug.txt') }
    if (-not $dialog) { throw 'RGB color picker did not open' }
    $cancel = $dialog.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'2'))
    if (-not $cancel) { throw 'Color picker Cancel button not found' }
    $cancel.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Start-Sleep -Milliseconds 300
    Set-Value 'Widget background color HEX' '#1f3a5f'
    Invoke-Control 'Widget background color — apply'
    Set-Value 'Text and battery color HEX' '#f5d547'
    Invoke-Control 'Text and battery color — apply'
    $saved = Get-Content -Raw $file | ConvertFrom-Json
    if ($saved.WidgetBackground -ne '#1F3A5F' -or $saved.WidgetForeground -ne '#F5D547') { throw 'Colors not saved' }
    Set-Value 'Widget background color HEX' '#ZZZZZZ'
    Invoke-Control 'Widget background color — apply'
    $saved = Get-Content -Raw $file | ConvertFrom-Json
    if ($saved.WidgetBackground -ne '#1F3A5F') { throw 'Invalid color was accepted' }
    Exit-App
    $process = Start-Process -FilePath $Executable -ArgumentList $TestArguments -PassThru -WindowStyle Hidden
    Wait-TestCondition { Find-Panel } 'details panel' | Out-Null
    $nav=(Find-Panel).FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'Nav1'))
    $nav.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    $value = (Find-Control 'Widget background color HEX').GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value
    if ($value -ne '#1F3A5F') { throw 'Color not restored after restart' }
    $condition = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$process.Id)
    $windows = [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,$condition)
    $widget = $windows | Where-Object { $_.Current.Name -ne 'ZETMAR Battery' } | Select-Object -First 1
    $rect = $widget.Current.BoundingRectangle
    $bmp = [System.Drawing.Bitmap]::new([int]$rect.Width,[int]$rect.Height)
    $g = [System.Drawing.Graphics]::FromImage($bmp); $dc = $g.GetHdc()
    try { [ColorWindowCapture]::PrintWindow([IntPtr]$widget.Current.NativeWindowHandle,$dc,2) | Out-Null } finally { $g.ReleaseHdc($dc) }
    $bmp.Save((Join-Path $PSScriptRoot '..\artifacts\widget-colors.png')); $g.Dispose(); $bmp.Dispose()
    Invoke-Control 'Restore automatic colors'
    $saved = Get-Content -Raw $file | ConvertFrom-Json
    if ($saved.WidgetBackground -or $saved.WidgetForeground) { throw 'Reset colors failed' }
    Exit-App
    'PASS: RGB picker open/cancel, custom background/text, HEX normalization, invalid input rejection, persistence across restart, reset'
} finally {
    try { Exit-App } finally { Stop-TestProcess $process }
}
