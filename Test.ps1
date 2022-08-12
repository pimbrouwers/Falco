[CmdletBinding()]
param (
    [ValidateSet("Core", "Markup")]
    [string] $Assembly,
    [Switch] $NoClean,
    [switch] $Watch
)

function RunCommand {
    param ([string] $CommandExpr)
    Write-Verbose "  $CommandExpr"
    Invoke-Expression $CommandExpr
}


$assemblyName = "Falco.Tests"

if ($Assembly -eq "Markup")
{ 
    $assemblyName = "Falco.Markup.Tests"
}

$assemblyPath = Join-Path -Path $PSScriptRoot -ChildPath "test\$assemblyName\"

if (!(Test-Path -Path $assemblyPath))
{
    throw "Invalid project"
}

if(!$NoClean)
{
    RunCommand -CommandExpr "dotnet clean --nologo --verbosity quiet"
}

if ($Watch) 
{
    RunCommand -CommandExpr "dotnet watch --project $assemblyPath -- test"
}
else 
{ 
    RunCommand -CommandExpr "dotnet test $assemblyPath"
}