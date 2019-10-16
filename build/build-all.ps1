param (
	[Parameter(Mandatory=$true)]
	[ValidatePattern("^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-(0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(\.(0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*)?(\+[0-9a-zA-Z-]+(\.[0-9a-zA-Z-]+)*)?$")]
	[string]
	$Version
)

./build.ps1 $Version "win-x64"
./build.ps1 $Version "win-x86"
./build.ps1 $Version "linux-x64"
./build.ps1 $Version "linux-arm"
