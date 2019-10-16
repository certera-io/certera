param (
	[Parameter(Mandatory=$true)]
	[string]
	$Root,
	
	[Parameter(Mandatory=$true)]
	[string]
	$PublishOutput,
	
	[Parameter(Mandatory=$true)]
	[string]
	$Runtime,
	
	[Parameter(Mandatory=$true)]
	[string]
	$Version
)

Add-Type -Assembly "system.io.compression.filesystem"
$Temp = "$Root\build\temp\"
$Artifacts = "$Root\build\artifacts\"

# Clean temp folder
if (Test-Path $Temp) 
{
    Remove-Item $Temp -Recurse
}
New-Item $Temp -Type Directory

if (!(Test-Path $Artifacts))
{
    New-Item $Artifacts -Type Directory
}

$ZipFile = "$Artifacts\certera-$Version-$Runtime.zip"
if (Test-Path $ZipFile) 
{
    Remove-Item $ZipFile
}

Copy-Item -Path "$PublishOutput\*" -Destination "$Temp" -recurse -Force -Verbose

Set-Content -Path "$Temp\version.txt" -Value "v$Version"

[io.compression.zipfile]::CreateFromDirectory($Temp, $ZipFile)
