param([string]$Executable=(Join-Path $PSScriptRoot '../src/Zetmar.Battery/bin/Release/net10.0-windows/ZETMAR Battery.exe'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes
. (Join-Path $PSScriptRoot 'Test-Support.ps1')
Initialize-TestProfile
$startupArgs=@('--startup','--data-dir',('"'+$TestProfile+'"'))
$process=$null;$duplicate=$null;$manual=$null
function Windows {
    [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$process.Id))
}
function Panel { Windows|Where-Object{$_.Current.Name -eq 'ZETMAR Battery'}|Select-Object -First 1 }
function Control([string]$Name){(Panel).FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,$Name))}
try{
    $process=Start-Process -FilePath $Executable -ArgumentList $startupArgs -PassThru -WindowStyle Hidden
    Wait-TestCondition {Windows} 'startup widget'|Out-Null
    if(Panel){throw 'Startup opened settings instead of the widget'}
    $duplicate=Start-Process -FilePath $Executable -ArgumentList $startupArgs -PassThru -WindowStyle Hidden
    if(-not $duplicate.WaitForExit(5000)){throw 'Duplicate startup instance did not exit'}
    if(Panel){throw 'Duplicate startup opened settings'}
    $manual=Start-Process -FilePath $Executable -ArgumentList $TestArguments -PassThru -WindowStyle Hidden
    Wait-TestCondition {Panel} 'manual launch opens settings'|Out-Null
    $nav=(Panel).FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'Nav4'))
    $nav.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    $repair=Wait-TestCondition {Control 'Repair startup'} 'startup settings page'
    $repair.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Wait-TestCondition {Control 'Startup points to the current application file.'} 'repaired registration status'|Out-Null
    $shortcut=Join-Path $TestProfile 'startup/ZETMAR Battery.lnk'
    if(-not (Test-Path -LiteralPath $shortcut)){throw 'Repair did not create real shortcut'}
    $shell=New-Object -ComObject WScript.Shell
    $link=$shell.CreateShortcut($shortcut)
    if($link.TargetPath -ne [System.IO.Path]::GetFullPath($Executable) -or $link.Arguments -ne '--startup' -or $link.WorkingDirectory -ne (Split-Path ([System.IO.Path]::GetFullPath($Executable)))){throw 'Incorrect shortcut properties'}
    $toggle=Control 'Start at sign-in'
    if(-not $toggle){throw 'Startup checkbox missing'}
    $toggle.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Toggle()
    Wait-TestCondition {-not (Test-Path -LiteralPath $shortcut)} 'disabled shortcut removed'|Out-Null
    (Control 'Start at sign-in').GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Toggle()
    Wait-TestCondition {Test-Path -LiteralPath $shortcut} 'enabled shortcut created'|Out-Null
    'PASS: startup widget, silent duplicate, manual panel; repair and checkbox create/remove real shortcut with correct target, arguments and working directory'
}finally{try{Stop-TestProcess $manual;Stop-TestProcess $duplicate}finally{Stop-TestProcess $process}}
