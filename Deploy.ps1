[CmdletBinding()]
param (
    [ValidateSet("Core", "Markup")]
    [string] $Assembly,

    [Parameter(Mandatory = $True, HelpMessage = "The NuGet API Key.")]
    [string] $ApiKey,

    [Parameter(Mandatory = $True, HelpMessage = "The NuGet Package Version.")]
    [string] $PackageVersion
)

$assemblyName = "Falco"

if ($Assembly -eq "Markup")
{
    $assemblyName = "Falco.Markup"
}

$assemblyPath = Join-Path -Path $PSScriptRoot -ChildPath "src\$assemblyName\"

if (!(Test-Path -Path $assemblyPath))
{
    throw "Invalid project"
}

$nugetPath = Join-Path -Path $assemblyPath -ChildPath "bin\Release\$assemblyName.$PackageVersion.nupkg"

if (!(Test-Path -Path $assemblyPath)) {
    throw "Invalid project"
}

function RunCommand {
    param ([string] $CommandExpr)
    Write-Verbose "  $CommandExpr"
    Invoke-Expression $CommandExpr
}

#
# Pack
RunCommand -CommandExpr "dotnet pack $assemblyPath -c Release --include-symbols"

#
# Publish
RunCommand -CommandExpr "dotnet nuget push $nugetPath -k `"$ApiKey`" -s https://api.nuget.org/v3/index.json"