[CmdletBinding()]
param (
    [Parameter(HelpMessage="The action to execute.")]
    [ValidateSet("Build", "Test", "Pack", "BuildSite", "DevelopSite")]
    [string] $Action = "Build",

    [Parameter(HelpMessage="The msbuild configuration to use.")]
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Debug",

    [switch] $SkipClean
)

function RunCommand {
    param ([string] $CommandExpr)
    Write-Verbose "  $CommandExpr"
    Invoke-Expression "$CommandExpr --no-restore"
}

$rootDir = $PSScriptRoot
$srcDir = Join-Path -Path $rootDir -ChildPath 'src'
$testDir = Join-Path -Path $rootDir -ChildPath 'test'

switch ($Action) {
    "Test"        { $projectdir = Join-Path -Path $testDir -ChildPath 'Falco.Tests' }
    "Pack"        { $projectDir = Join-Path -Path $srcDir -ChildPath 'Falco' }
    "BuildSite"   { $projectDir = Join-Path -Path $rootDir -ChildPath 'site' }
    "DevelopSite" { $projectDir = Join-Path -Path $rootDir -ChildPath 'site' }
    Default       { $projectDir = Join-Path -Path $srcDir -ChildPath 'Falco' }
}

if (!$SkipClean.IsPresent)
{
    # RunCommand "dotnet restore $projectDir --force --force-evaluate --nologo --verbosity quiet"
    # RunCommand "dotnet clean $projectDir -c $Configuration --nologo --verbosity quiet"
}

switch ($Action) {
    "Test"        { RunCommand "dotnet test `"$projectDir`"" }
    "Pack"        { RunCommand "dotnet pack `"$projectDir`" -c $Configuration --include-symbols --include-source" }
    "BuildSite"   { RunCommand "dotnet build `"$projectDir`" -t:Generate" }
    "DevelopSite" { RunCommand "dotnet build `"$projectDir`" -t:Develop" }
    Default       { RunCommand "dotnet build `"$projectDir`" -c $Configuration" }
}