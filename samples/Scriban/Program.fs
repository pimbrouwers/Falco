module Falco.Scriban.Program

open System
open System.IO
open System.Threading.Tasks
open Falco
open Falco.Routing
open Falco.HostBuilder
open Scriban

type RenderTemplate = string -> obj -> ValueTask<string>

// ------------
// Pages
// ------------
module Pages =
    let homepage (renderTemplate : RenderTemplate) : HttpHandler =
        let queryMap (q: QueryCollectionReader) =
            {| Name = q.Get "name" |}

        let next model : HttpHandler = fun ctx ->
            task {
                let! html = renderTemplate "Home" model
                return Response.ofHtmlString html ctx
            }

        Request.mapQuery queryMap next

// ------------
// App
// ------------
module Template =
    let loadFrom (path : string) =
        Directory.EnumerateFiles(path)
        |> Seq.map (fun file ->
            let viewName = Path.GetFileNameWithoutExtension(file)
            let viewContent = File.ReadAllText(file)
            let view = Template.Parse(viewContent)
            viewName, view)
        |> Map.ofSeq

    let render (templates : Map<string, Template>) (name : string) (model : obj) =
        match Map.tryFind name templates with
        | Some template -> template.RenderAsync(model)
        | None -> failwithf "Template '%s' was not found" name

[<EntryPoint>]
let main args =
    let executionDirectory = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0])
    let scribanTemplates = Template.loadFrom (Path.Combine(executionDirectory, "Views"))
    let renderTemplate = Template.render scribanTemplates

    webHost args {
        use_https

        endpoints [
            get "/" (Pages.homepage renderTemplate)
        ]
    }

    0