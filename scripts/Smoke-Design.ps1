param([string]$Executable=(Join-Path $PSScriptRoot '../src/Zetmar.Battery/bin/Release/net10.0-windows/ZETMAR Battery.exe'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes,System.Drawing
Add-Type -TypeDefinition 'using System;using System.Runtime.InteropServices;public static class DesignCapture{[DllImport("user32.dll")]public static extern bool PrintWindow(IntPtr h,IntPtr dc,uint f);}'
. (Join-Path $PSScriptRoot 'Test-Support.ps1')
function Panel {
    [System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.AndCondition]::new([System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$process.Id),[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'ZETMAR Battery')))
}
function Capture([string]$Name){
    Start-Sleep -Milliseconds 200
    $window=Panel;$r=$window.Current.BoundingRectangle;$bmp=[System.Drawing.Bitmap]::new([int]$r.Width,[int]$r.Height);$g=[System.Drawing.Graphics]::FromImage($bmp);$dc=$g.GetHdc()
    try{if(-not [DesignCapture]::PrintWindow([IntPtr]$window.Current.NativeWindowHandle,$dc,2)){throw 'Capture failed'}}finally{$g.ReleaseHdc($dc)}
    try{$bmp.Save((Join-Path $TestOutput $Name))}finally{$g.Dispose();$bmp.Dispose()}
}
foreach($theme in @('light','dark')){
    Initialize-TestProfile
    [System.IO.File]::WriteAllText((Join-Path $TestProfile 'settings.json'),('{"Language":"pl","Theme":"'+$theme+'","Estimate":true}'))
    $process=$null
    try{
        $process=Start-Process -FilePath $Executable -ArgumentList $TestArguments -PassThru -WindowStyle Hidden
        Wait-TestCondition {Panel} 'design panel'|Out-Null
        foreach($page in 0..4){
            $item=(Panel).FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,('Nav'+$page)))
            if(-not $item){throw "Navigation missing: $page"}
            $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
            Capture "design-$theme-$page.png"
        }
        (Panel).GetCurrentPattern([System.Windows.Automation.TransformPattern]::Pattern).Resize(640,600)
        $item=(Panel).FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'Nav1'))
        $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
        $font=(Panel).FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.AndCondition]::new([System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'Rozmiar tekstu'),[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::ComboBox)))
        $font.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
        $size=Wait-TestCondition { $font.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.AndCondition]::new([System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'22'),[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::ListItem))) } 'font dropdown option'
        $size.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
        Wait-TestCondition { (Get-Content -Raw (Join-Path $TestProfile 'settings.json')|ConvertFrom-Json).FontSize -eq 22 } 'font setting saved'|Out-Null
        Capture "design-$theme-compact.png"
        (Panel).GetCurrentPattern([System.Windows.Automation.TransformPattern]::Pattern).Resize(680,400)
        foreach($page in 0..4){
            $item=(Panel).FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,('Nav'+$page)))
            $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
        }
        Capture "design-$theme-short.png"
        "PASS: $theme navigation, five pages, 640x600 / 680x400 layouts and screenshots"
    }finally{Stop-TestProcess $process}
}
