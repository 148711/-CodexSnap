$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
& $compiler /nologo /target:exe /out:"$PSScriptRoot\SettingsTests.exe" /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:"$framework\WPF\UIAutomationClient.dll" /reference:"$framework\WPF\UIAutomationTypes.dll" /reference:"$framework\WPF\WindowsBase.dll" "$PSScriptRoot\SettingsTests.cs" "$PSScriptRoot\Settings.cs" "$PSScriptRoot\Selection.cs" "$PSScriptRoot\Design.cs" "$PSScriptRoot\Native.cs" "$PSScriptRoot\ChatInput.cs"
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
& "$PSScriptRoot\SettingsTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
