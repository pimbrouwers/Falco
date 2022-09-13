[CmdletBinding()]
param (
    [Parameter(HelpMessage="The action to execute.")]
    [ValidateSet("Build", "Test", "Pack")]
    [string] $Action = "Build",

    [Parameter(HelpMessage="The msbuild configuration to use.")]
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Debug",

    [switch] $SkipClean
)

function RunCommand {
    param ([string] $CommandExpr)
    Write-Verbose "  $CommandExpr"
    Invoke-Expression $CommandExpr
}

$rootDir = $PSScriptRoot
$srcDir = Join-Path -Path $rootDir -ChildPath 'src'
$testDir = Join-Path -Path $rootDir -ChildPath 'test'
$projectDir = Join-Path -Path $srcDir -ChildPath 'Falco'

if ($Action -eq "Test")
{
    $projectdir = Join-Path -Path $testDir -ChildPath 'Falco.Tests'
}

if (!$SkipClean)
{
    RunCommand "dotnet restore $projectDir --force --force-evaluate --nologo --verbosity quiet"
    RunCommand "dotnet clean $projectDir -c $Configuration --nologo --verbosity quiet"
}

switch ($Action) {
    "Test"  { RunCommand "dotnet test `"$projectDir`"" }
    "Pack"  { RunCommand "dotnet pack `"$projectDir`" -c $Configuration --include-symbols --include-source" }
    Default { RunCommand "dotnet build `"$projectDir`" -c $Configuration" }
}