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

#
# Output
$outputDir = Join-Path -Path $PSScriptRoot -ChildPath ..\docs

if (Test-Path -Path $outputDir) {
    Write-Verbose "Clobbering output dir"
    Remove-Item $outputDir -Recurse -Force 
}

Write-Verbose "Creating output dir"
New-Item -ItemType Directory $outputDir | Write-Verbose

#
# CNAME
Write-Verbose "Copying CNAME"
Copy-Item -Path (Join-Path -Path $PSScriptRoot -ChildPath ..\CNAME) -Destination $outputDir

#
# Assets
$assetsDir = Join-Path -Path $PSScriptRoot -ChildPath assets\*
Write-Verbose "Copying Assets"
Copy-Item -Path $assetsDir -Destination $outputDir -Recurse -Container

#
# Pages
$copyrightYear = Get-Date -Format "yyyy"
$pagesDir = Join-Path -Path $PSScriptRoot -ChildPath pages
$templatesDir = Join-Path -Path $PSScriptRoot -ChildPath templates

Write-Verbose "Loading layout"
$layout = Get-Content -Path (Join-Path -Path $PSScriptRoot -ChildPath templates\layout.html) -Raw | Out-String

Write-Verbose "Loading partials"
$features = Get-Content -Path (Join-Path -Path $PSScriptRoot -ChildPath templates\features.html) -Raw | Out-String
$hero = Get-Content -Path (Join-Path -Path $PSScriptRoot -ChildPath templates\hero.html) -Raw | Out-String
$pageHeading = Get-Content -Path (Join-Path -Path $PSScriptRoot -ChildPath templates\page-heading.html) -Raw | Out-String

Write-Verbose "Creating 404 page"
$markdown = Get-Content -Raw -Path (Join-Path -Path $pagesDir -ChildPath 404.md) | ConvertFrom-Markdown

Invoke-Template {  
    $title = "404 - Not Found | Falco Framework"
    $pageTitle = "Not Found"  
    $pageDescription = "Something went wrong."
  
    $topHtmlContent = Render-Template $pageHeading

    Render-Template $layout
    | Out-File -Path (Join-Path -Path $outputDir -ChildPath "404.html")
    | Write-Verbose
}

# Write-Verbose "Creating pages"
# Get-ChildItem -Path (Join-Path -Path $PSScriptRoot -ChildPath pages) -Exclude 404.md | ForEach-Object {
#     Write-Verbose "Creating page: $($_.Name)"
#     $markdown = Get-Content -Raw -Path $_.FullName | ConvertFrom-Markdown
    
#     Invoke-Template {

#         if ($_.Name -eq "index.md") {
#             $topHtmlContent = Render-Template $hero                        
#         } 
        
#         $htmlContent = $markdown | Select-Object -ExpandProperty Html  
    
#         Render-Template $layout 
#         | Out-File -Path (Join-Path -Path $outputDir -ChildPath $_.Name.Replace(".md", ".html"))
#         | Write-Verbose
#     }
# }

Write-Verbose "Creating index page"
$readmePath = Join-Path -Path $PSScriptRoot -ChildPath "../README.md"
$markdown = Get-Content -Raw -Path $readmePath | ConvertFrom-Markdown

Invoke-Template {

    $topHtmlContent = Render-Template $hero                        

    Write-Debug $topHtmlContent

    $htmlContent = $markdown | Select-Object -ExpandProperty Html

    Write-Debug $htmlContent

    Render-Template $layout 
    | Out-File -Path (Join-Path -Path $outputDir -ChildPath "index.html")
    | Write-Verbose
}