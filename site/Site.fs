open System
open System.IO
open System.Linq
open System.Text.RegularExpressions
open Markdig
open Markdig.Extensions.Yaml
open Markdig.Renderers
open Markdig.Syntax
open Scriban

module Log =
    let private log kind fmt =
        Printf.kprintf (fun s ->
            let now = DateTime.Now
            let msg = sprintf "[%s] [%s] %s" (now.ToString("s")) kind s

            printfn "%s" msg) fmt

    let fail fmt = log "Fail" fmt

    let info fmt = log "Info" fmt

module Path =
    let resolve (childPath : string) =
        Path.Join(__SOURCE_DIRECTORY__, childPath)

module Directory =
    let copyRecursive (destinationDir : DirectoryInfo) (sourceDir : DirectoryInfo) =
        let rec copy (destDir : DirectoryInfo) (srcDir : DirectoryInfo) =
            for subDir in srcDir.GetDirectories() do
                let subDestDir = DirectoryInfo(Path.Join(destDir.FullName, subDir.Name))
                copy subDestDir subDir

            for file in srcDir.GetFiles() do
                if not(destDir.Exists) then destDir.Create()
                File.Copy(file.FullName, Path.Join(destDir.FullName, file.Name))

        copy destinationDir sourceDir


type ParsedMarkdownDocument =
    { Title : string
      Body  : string }

module Markdown =
    let render (markdown : string) =
        let pipeline =
            MarkdownPipelineBuilder()
                .UseAutoIdentifiers()
                .UsePipeTables()
                .UseYamlFrontMatter()
                .Build()

        use sw = new StringWriter()
        let renderer = HtmlRenderer(sw)

        pipeline.Setup(renderer) |> ignore

        let doc = Markdown.Parse(markdown, pipeline)

        renderer.Render(doc) |> ignore
        sw.Flush() |> ignore
        let renderedMarkdown = sw.ToString()

        let frontmatter =
            match doc.Descendants<YamlFrontMatterBlock>().FirstOrDefault() with
            | null -> Map.empty
            | yamlBlock ->
                markdown
                    .Substring(yamlBlock.Span.Start, yamlBlock.Span.End)
                    .Split("\r")
                |> Array.tail
                |> Array.rev
                |> Array.tail
                |> Array.map (fun x ->
                    let keyValue = x.Split(":")
                    keyValue.[0].Trim(), keyValue.[1].Trim())
                |> Map.ofArray

        // rewrite markdown links
        let body = Regex.Replace(renderedMarkdown, "([a-zA-Z\-]+)\.md", "$1.html")

        { Title = Map.tryFind "title" frontmatter |> Option.defaultValue ""
          Body  = body }

    let renderFile (path : string) =
        render (File.ReadAllText(path))

type LayoutTemplate =
    { Title : string
      MainContent : string
      CopyrightYear : int }

type LayoutTwoColTemplate =
    { Title : string
      SideContent : string
      MainContent : string
      CopyrightYear : int }

type PartialTemplates(workingDir : string) =
    member _.DocsLinks =
        Path.Join(workingDir, "templates/partials/docs-links.html")
        |> File.ReadAllText
        |> Template.Parse
        |> fun t -> t.Render()

    member _.DocsNav =
        Path.Join(workingDir, "templates/partials/docs-nav.html")
        |> File.ReadAllText
        |> Template.Parse
        |> fun t -> t.Render()

type Templates(workingDir : string) =
    let layoutTmpl =
        Path.Join(workingDir, "templates/layout.html")
        |> File.ReadAllText
        |> Template.Parse

    let layoutTwoColTmpl =
        Path.Join(workingDir, "templates/layout-twocol.html")
        |> File.ReadAllText
        |> Template.Parse


    member _.Layout (template : LayoutTemplate) =
        layoutTmpl.Render(template)

    member _.LayoutTwoCol (template : LayoutTwoColTemplate) =
        layoutTwoColTmpl.Render(template)

    member _.Partials = PartialTemplates(workingDir)

module Docs =
    let build (template : Templates) (docs : FileInfo[]) (buildDir : DirectoryInfo) =
        if not(buildDir.Exists) then buildDir.Create()

        for file in docs do
            let buildFilename, sideContent =
                if file.Name = "readme.md" then
                    "index.html", template.Partials.DocsLinks
                else
                    Path.ChangeExtension(file.Name, ".html"), template.Partials.DocsNav

            let mainContent = Markdown.renderFile file.FullName

            let html =
                { Title = mainContent.Title
                  SideContent = sideContent
                  MainContent = mainContent.Body
                  CopyrightYear = DateTime.Now.Year }
                |> template.LayoutTwoCol

            File.WriteAllText(Path.Join(buildDir.FullName, buildFilename), html)

[<EntryPoint>]
let main args =
    if args.Length <> 1 then failwith "Must provide the working directory as the first argument"
    let workingDir = DirectoryInfo(args.[0])
    let template = Templates(workingDir.FullName)

    // Clean build
    Log.info "Clearing build directory..."
    let buildDir = Directory.CreateDirectory(Path.Join(workingDir.FullName, "build"))
    if buildDir.Exists then buildDir.Delete(recursive = true)

    // Copy asset
    Log.info "Copying assets..."
    let assetsDir = DirectoryInfo(Path.Join(workingDir.FullName, "assets"))
    Directory.copyRecursive buildDir assetsDir

    // Render homepage
    Log.info "Rendering homepage..."
    let homepageFilename = Path.Join(workingDir.FullName, "../README.md")
    let mainContent = Markdown.renderFile homepageFilename


    { Title = String.Empty
      MainContent = mainContent.Body
      CopyrightYear = DateTime.Now.Year }
    |> template.Layout
    |> fun text -> File.WriteAllText(Path.Join(buildDir.FullName, "index.html"), text)

    // Render docs
    let docsDir = DirectoryInfo(Path.Join(workingDir.FullName, "../docs"))
    let docsBuildDir = Directory.CreateDirectory(Path.Join(buildDir.FullName, "docs"))

    Log.info "Rendering docs..."
    Docs.build template (docsDir.GetFiles()) docsBuildDir

    // Additional languages
    let languageCodes = []

    for languageCode in languageCodes do
        Log.info "Rendering /%s docs" languageCode
        let languageDir = DirectoryInfo(Path.Join(docsDir.FullName, languageCode))
        let languageBuildDir = DirectoryInfo(Path.Join(docsBuildDir.FullName, languageCode))
        Docs.build template (languageDir.GetFiles()) languageBuildDir

    0