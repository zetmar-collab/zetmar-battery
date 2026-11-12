param([string]$Executable=(Join-Path $PSScriptRoot '../src/Zetmar.Battery/bin/Release/net10.0-windows/ZETMAR Battery.exe'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes,System.Drawing
Add-Type -TypeDefinition 'using System;using System.Runtime.InteropServices;public static class FeatureCapture{[DllImport("user32.dll")]public static extern bool PrintWindow(IntPtr h,IntPtr dc,uint f);}'
. (Join-Path $PSScriptRoot 'Test-Support.ps1')
Initialize-TestProfile
[System.IO.File]::WriteAllText((Join-Path $TestProfile 'settings.json'),'{"Language":"en","HistoryEnabled":false,"Theme":"light"}')
$fixture=@()
for($i=120;$i -ge 0;$i--){$fixture += [pscustomobject]@{At=[DateTimeOffset]::UtcNow.AddMinutes(-$i).ToString('O');Percent=70+[int][math]::Floor($i/10);State=3}}
[System.IO.File]::WriteAllText((Join-Path $TestProfile 'history.json'),($fixture|ConvertTo-Json))
$process=$null
function Window-ByName([string]$Name){
    [System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.AndCondition]::new([System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$process.Id),[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,$Name)))
}
function Control-ByName([string]$Name){
    (Window-ByName 'ZETMAR Battery').FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,$Name))
}
function Capture-Window($window,[string]$Name){
    $r=$window.Current.BoundingRectangle;$bmp=[System.Drawing.Bitmap]::new([int]$r.Width,[int]$r.Height);$g=[System.Drawing.Graphics]::FromImage($bmp);$dc=$g.GetHdc()
    try{if(-not [FeatureCapture]::PrintWindow([IntPtr]$window.Current.NativeWindowHandle,$dc,2)){throw 'Capture failed'}}finally{$g.ReleaseHdc($dc)}
    try{$bmp.Save((Join-Path $TestOutput $Name))}finally{$g.Dispose();$bmp.Dispose()}
}
try{
    $process=Start-Process -FilePath $Executable -ArgumentList $TestArguments -PassThru -WindowStyle Hidden
    Wait-TestCondition {Window-ByName 'ZETMAR Battery'} 'details'|Out-Null
    $historyFile=Join-Path $TestProfile 'history.json'
    $before=(Get-FileHash -LiteralPath $historyFile).Hash
    $duplicate=Start-Process -FilePath $Executable -ArgumentList $TestArguments -PassThru -WindowStyle Hidden
    try{if(-not $duplicate.WaitForExit(5000)){throw 'Duplicate instance did not exit'}}finally{Stop-TestProcess $duplicate}
    if((Get-FileHash -LiteralPath $historyFile).Hash -ne $before){throw 'Duplicate instance wrote history'}
    $nav=(Window-ByName 'ZETMAR Battery').FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'Nav3'))
    $nav.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    $section=Control-ByName 'History and battery health'
    $section.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
    (Control-ByName 'Read battery health').GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    $health=Wait-TestCondition {
        $all=(Window-ByName 'ZETMAR Battery').FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)
        $all|Where-Object {$_.Current.Name -like '*Design capacity:*' -or $_.Current.Name -like 'Data unavailable:*'}|Select-Object -First 1
    } 'battery health result' 30
    $health.Current.Name|Set-Content -Encoding utf8 (Join-Path $TestOutput 'health-result.txt')
    if($health.Current.Name -like 'Data unavailable:*'){throw 'Hardware health read failed'}
    (Control-ByName 'Show history chart').GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    $chart=Wait-TestCondition {Window-ByName 'Battery history'} 'history chart'
    Start-Sleep -Milliseconds 300
    Capture-Window $chart 'history.png'
    $chart.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()
    Capture-Window (Window-ByName 'ZETMAR Battery') 'panel-1.2.png'
    'PASS: duplicate instance leaves history intact; live Windows battery health and history chart from isolated fixture'
}finally{Stop-TestProcess $process}
