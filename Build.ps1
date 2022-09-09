[CmdletBinding()]
param (
    [Parameter(HelpMessage="The process to execute.")]
    [ValidateSet("Build", "Test", "Pack")]
    [string] $Process = "Build",

    [Parameter(HelpMessage="The msbuild configuration to use.")]
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Debug"
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

if ($Process -eq "Test")
{
    $projectdir = Join-Path -Path $testDir -ChildPath 'Falco.Tests'
}

RunCommand 'dotnet restore src --force --force-evaluate --nologo --verbosity quiet'
RunCommand "dotnet clean $projectDir -c $Configuration --nologo --verbosity quiet"

switch ($Process) {
    "Test"  { RunCommand "dotnet test `"$projectDir`"" }
    "Pack"  { RunCommand "dotnet pack `"$projectDir`" -c $Configuration --include-symbols --include-source" }
    Default { RunCommand "dotnet build `"$projectDir`" -c $Configuration" }
}