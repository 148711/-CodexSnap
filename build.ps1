$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /win32manifest:"$PSScriptRoot\app.manifest" /win32icon:"$PSScriptRoot\app.ico" /resource:"$PSScriptRoot\app.ico",AppIcon /resource:"$PSScriptRoot\app-icon.png",AppLogo /out:"$PSScriptRoot\CodexSnap.exe" /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll /resource:"$PSScriptRoot\locales\en.json",EnglishStrings /reference:System.Drawing.dll /reference:"$framework\WPF\UIAutomationClient.dll" /reference:"$framework\WPF\UIAutomationTypes.dll" /reference:"$framework\WPF\WindowsBase.dll" "$PSScriptRoot\CodexSnap.cs" "$PSScriptRoot\MainForm.cs" "$PSScriptRoot\Native.cs" "$PSScriptRoot\Selection.cs" "$PSScriptRoot\Design.cs" "$PSScriptRoot\Settings.cs" "$PSScriptRoot\ChatInput.cs" "$PSScriptRoot\L10n.cs"
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
Write-Output "Built $PSScriptRoot\CodexSnap.exe"
