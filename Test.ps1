[CmdletBinding()]
param ()

function RunCommand {
    param ([string] $CommandExpr)
    Write-Verbose "  $CommandExpr"
    Invoke-Expression $CommandExpr
}

RunCommand -CommandExpr "dotnet clean -c Debug --nologo --verbosity quiet"

$assemblies = "Falco.Tests", "Falco.Markup.Tests"

foreach ($assemblyName in $assemblies) 
{
    $assemblyPath = Join-Path -Path $PSScriptRoot -ChildPath "test\$assemblyName\"
    
    if(!(Test-Path -Path $assemblyPath))
    {
        throw "Invalid project"
    }
            
    RunCommand -CommandExpr "dotnet test $assemblyPath"
}
