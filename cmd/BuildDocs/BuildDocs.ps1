[CmdletBinding()]
param()
function Invoke-Template {
	param([ScriptBlock] $scriptBlock)

  function Render-Template {
		param([string] $template)    

		Invoke-Expression "@`"`r`n$template`r`n`"@"
	}
	& $scriptBlock
}

$rootDir = Join-Path -Path $PSScriptRoot -ChildPath ..\..
$outputDir = Join-Path -Path $rootDir -ChildPath docs
$template = Get-Content -Path (Join-Path -Path $PSScriptRoot -ChildPath template.html) -Raw | Out-String

if(Test-Path -Path $outputDir) {
  Remove-Item $outputDir -Recurse -Force 
}

New-Item -ItemType Directory $outputDir | Write-Verbose

#
# Copy cruft
'..\..\CNAME', 'prism.css', 'prism.js' | ForEach-Object { 
  Copy-Item -Path (Join-Path -Path $PSScriptRoot -ChildPath $_) -Destination $outputDir
}

#
# 404
Invoke-Template {  
  $title = "404 - Not Found | Falco Framework"
  $markdown = Get-Content -Raw -Path (Join-Path -Path $PSScriptRoot -ChildPath 404.md) | ConvertFrom-Markdown    

  $htmlContent = $markdown | Select-Object -ExpandProperty Html  

  Write-Debug $htmlContent

  Render-Template $template 
  | Out-File -Path (Join-Path -Path $outputDir -ChildPath "404.html")
  | Write-Verbose
}

#
# Index
Invoke-Template {  
  $rawReadme = Get-Content -Path (Join-Path -Path $rootDir -ChildPath README.md)
  $cleanReadme = $rawReadme | Where-Object { ($_ -ne "# Falco") -and ($_ -notlike "``[``!``[NuGet*") -and ($_ -notlike "``[``!``[Build*") }
  $markdown = ($cleanReadme -join "`n") | ConvertFrom-Markdown     
  $htmlContent = $markdown | Select-Object -ExpandProperty Html
  
  Write-Debug $htmlContent
  
  $title = "Falco Framework"

  Render-Template $template 
  | Out-File -Path (Join-Path -Path $outputDir -ChildPath "index.html")
  | Write-Verbose
}
