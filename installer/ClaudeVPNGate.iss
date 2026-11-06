#define MyAppName "Claude VPN Gate"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "ZVatnikV"
#define MyAppExeName "ClaudeVPNGate.exe"
#define MyAppId "{{C4E8A1D2-6B37-4F90-8C15-2A9D7E0B44F1}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://github.com/ZVatnikV/ClaudeVPNGate
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\dist\installer
OutputBaseFilename=ClaudeVPNGate-Setup-{#MyAppVersion}
SetupIconFile=..\src\ClaudeVPNGate\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\dist\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "schtasks"; Parameters: "/Delete /TN ClaudeVPNGate /F"; Flags: runhidden
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""ClaudeVPNGate Outbound"""; Flags: runhidden
Filename: "powershell"; Parameters: "-NoProfile -Command ""$p=Join-Path $env:SystemRoot 'System32\drivers\etc\hosts'; $t=Get-Content $p -Raw; $t=[regex]::Replace($t,'(?s)# BEGIN ClaudeVPNGate.*?# END ClaudeVPNGate\r?\n',''); Set-Content $p $t -NoNewline"""; Flags: runhidden
