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
    (Control 'Repair startup').GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Wait-TestCondition {Control 'Startup points to the current application file.'} 'repaired registration status'|Out-Null
    'PASS: startup launches widget, duplicate startup stays silent, manual launch opens panel, isolated repair button registers current EXE'
}finally{try{Stop-TestProcess $manual;Stop-TestProcess $duplicate}finally{Stop-TestProcess $process}}
