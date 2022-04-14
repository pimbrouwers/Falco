[CmdletBinding()]
param (    
    [switch] $Watch
)

function RunCommand {
    param ([string] $CommandExpr)
    Write-Verbose "  $CommandExpr"
    Invoke-Expression $CommandExpr
}

$assemblyName = "Falco.Tests"
$assemblyPath = Join-Path -Path $PSScriptRoot -ChildPath "test\$assemblyName\"

if(!(Test-Path -Path $assemblyPath))
{
    throw "Invalid project"
}

RunCommand -CommandExpr "dotnet clean $assemblyPath -c Debug --nologo --verbosity quiet"


if($Watch) {
    RunCommand -CommandExpr "dotnet watch --project $assemblyPath -- test"
}
else {
    RunCommand -CommandExpr "dotnet test $assemblyPath"
}