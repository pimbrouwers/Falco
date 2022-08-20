#I __SOURCE_DIRECTORY__
#r "nuget: Markdig"
#r "nuget: Scriban"

open System
open System.IO
open Markdig
open Markdig.Renderers
open Markdig.Syntax
open Scriban

module Path =
    let resolve (childPath : string) =
        Path.Join(__SOURCE_DIRECTORY__, childPath)

module File =
    let readAllText (path : string) =
        File.ReadAllText(path)

type ParsedMarkdownDocument =
    { Title : string
      Body  : string }

module Markdown =
    let render (markdown : string) =
        let pipeline = MarkdownPipelineBuilder().Build()
        use sw = new StringWriter()
        let renderer = HtmlRenderer(sw)

        pipeline.Setup(renderer) |> ignore

        let doc = Markdown.Parse(markdown, pipeline)

        renderer.Render(doc) |> ignore
        sw.Flush() |> ignore

        let body = sw.ToString()

        { Title = ""
          Body  = body }

    let renderFile (path : string) =
        render (File.ReadAllText(path))

type LayoutTemplate =
    { TopContent : string
      MainContent : string
      CopyrightYear : int }

module Template =
    let private layoutTmpl =
        Path.resolve "templates/layout.html"
        |> File.readAllText
        |> Template.Parse

    let layout (template : LayoutTemplate) =
        layoutTmpl.Render(template)

    let hero =
        Path.resolve "templates/hero.html"
        |> File.readAllText
        |> Template.Parse
        |> fun template -> template.Render()

let x = Path.resolve "content/en/get-started.md" |> Markdown.renderFile

// let x =
//     { TopContent = Templates.hero
//       MainContent = ""
//       CopyrightYear = 2022 }
//     |> Templates.layout
printfn "%A" x