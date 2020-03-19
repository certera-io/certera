param (
	[Parameter(Mandatory=$true)]
	[ValidatePattern("^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-(0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(\.(0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*)?(\+[0-9a-zA-Z-]+(\.[0-9a-zA-Z-]+)*)?$")]
	[string]
	$Version,
	
	[ValidateSet("win-x64","win-x86","win-arm","osx-x64","linux-x64","linux-arm")]
	[string]
	$Runtime
)

$PSScriptFilePath = Get-Item $MyInvocation.MyCommand.Path
$RepoRoot = $PSScriptFilePath.Directory.Parent.FullName
$SolutionPath = Join-Path -Path $RepoRoot -ChildPath "src\Certera.sln"
$ProjectPath = Join-Path -Path $RepoRoot "src\Certera.Web\Certera.Web.csproj"
$PublishOutput = Join-Path -Path $RepoRoot "src\publish\$Runtime"
$Profile = Join-Path -Path $RepoRoot "src\Certera.Web\Properties\PublishProfiles\template.pubxml"

$CleanVersion = $Version
$VersionSuffix = ""
if ($Version.Contains('-')) {
	$CleanVersion = $Version.Substring(0, $Version.IndexOf('-'))
	$VersionSuffix = $Version.Substring($Version.IndexOf('-'))
}

# Set the version in the csproj
$xml = [Xml] (Get-Content $ProjectPath)
$xml.Project.PropertyGroup.Version = $CleanVersion
$xml.Project.PropertyGroup.AssemblyVersion = $CleanVersion
$xml.Project.PropertyGroup.FileVersion = $CleanVersion
$xml.Save($ProjectPath);

Write-Output "PublishOutput: $PublishOutput"
Write-Output "Profile: $Profile"

# Go get nuget.exe if we don't have it
$NuGet = "nuget.exe"
$FileExists = Test-Path $NuGet 
If ($FileExists -eq $False) {
	$SourceNugetExe = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
	Invoke-WebRequest $SourceNugetExe -OutFile $NuGet
}

# Restore NuGet packages
.\nuget.exe restore $SolutionPath

# TODO: use PS Core and $IsLinux, $IsWindows, etc.
$ReadyToRun = "false"
if ($Runtime.StartsWith("win")) {
	$ReadyToRun = "true"
}

dotnet publish $ProjectPath -c Release -o "$PublishOutput" /p:PublishProfile="$Profile" /p:Version=$Version /p:AssemblyVersion=$CleanVersion /p:FileVersion=$CleanVersion /p:ReadyToRun=$ReadyToRun /p:RuntimeIdentifier=$Runtime

./zip.ps1 $RepoRoot "$PublishOutput" $RunTime $Version