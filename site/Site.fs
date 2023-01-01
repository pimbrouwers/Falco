open System
open System.IO
open System.Linq
open System.Net.Http
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
    use http = new HttpClient()

    // Clean build
    Log.info "Clearing build directory..."
    let buildDirPath = DirectoryInfo(Path.Join(workingDir.FullName, "../docs"))
    if buildDirPath.Exists then buildDirPath.Delete (recursive = true)
    else buildDirPath.Create ()

    // Copy asset
    Log.info "Copying assets..."
    let assetsDir = DirectoryInfo(Path.Join(workingDir.FullName, "assets"))
    Directory.copyRecursive buildDirPath assetsDir

    // Render homepage
    let template = Templates(workingDir.FullName)

    Log.info "Rendering homepage..."
    let indexMarkdown = Path.Join(workingDir.FullName, "../README.md") |> File.ReadAllText
    // let indexMarkdown = http.GetStringAsync(@"https://raw.githubusercontent.com/pimbrouwers/Falco/master/README.md").Result
    let mainContent = Markdown.render indexMarkdown

    { Title = String.Empty
      MainContent = mainContent.Body
      CopyrightYear = DateTime.Now.Year }
    |> template.Layout
    |> fun text -> File.WriteAllText(Path.Join(buildDirPath.FullName, "index.html"), text)

    // Render docs
    let docsDir = DirectoryInfo(Path.Join(workingDir.FullName, "../documentation"))
    let docsBuildDir = DirectoryInfo(Path.Join(buildDirPath.FullName, "docs"))

    Log.info "Downloading external markdown files..."
    let markupMarkdown = http.GetStringAsync(@"https://raw.githubusercontent.com/pimbrouwers/Falco.Markup/master/README.md").Result
    let markupFilename = Path.Join(docsDir.FullName, "markup.md")
    if (File.Exists(markupFilename)) then File.Delete(markupFilename)
    File.WriteAllText(markupFilename, markupMarkdown)

    Log.info "Rendering docs..."
    let readme = FileInfo(Path.Join(workingDir.FullName, "../readme.md"))
    let docFiles = Array.append [|readme|] (docsDir.GetFiles("*.md"))
    Docs.build template docFiles docsBuildDir

    // Additional languages
    let languageCodes = []

    for languageCode in languageCodes do
        Log.info "Rendering /%s docs" languageCode
        let languageDir = DirectoryInfo(Path.Join(docsDir.FullName, languageCode))
        let languageBuildDir = DirectoryInfo(Path.Join(docsBuildDir.FullName, languageCode))
        Docs.build template (languageDir.GetFiles()) languageBuildDir

    0