; Inno Setup starter script
[Setup]
AppName=Smart Storage Optimizer
AppVersion=0.1.0
DefaultDirName={autopf}\SmartStorageOptimizer
DefaultGroupName=SmartStorageOptimizer
OutputDir=artifacts
OutputBaseFilename=SmartStorageOptimizer-Setup
Compression=lzma
SolidCompression=yes

[Files]
; Add published files here after dotnet publish
; Source: "publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion
