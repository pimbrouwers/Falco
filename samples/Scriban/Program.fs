module HelloWorld.Program

open Falco
open Falco.Markup
open Falco.Routing
open Falco.HostBuilder
open Microsoft.FSharp.Reflection
open System.IO
open System.Reflection
open Scriban
open Scriban.Runtime
open FSharp.Control.Tasks

type View = | Home

let readViews () =
    let dir =
        FileInfo(Assembly.GetExecutingAssembly().Location)
            .Directory

    typeof<View>
    |> FSharpType.GetUnionCases
    |> Array.map (fun key -> key.Name, File.ReadAllText $"{dir}/Views/{key.Name}.html")
    |> Map.ofArray

let renderScriban model view =
    let template = Template.Parse view
    let scriptObject = ScriptObject()
    box model |> scriptObject.Import

    let context = TemplateContext()

    context.PushGlobal scriptObject
    template.RenderAsync context

let homeHandler views: HttpHandler =
    let next model: HttpHandler =
        fun ctx ->
            task {
                let view = views |> Map.find (nameof Home)
                let! html = renderScriban model view
                return! Response.ofHtmlString html ctx
            }

    let queryMap (q: QueryCollectionReader) = {| Name = q.Get "name" "World" |}

    Request.mapQuery queryMap next

[<EntryPoint>]
let main args =
    let views = readViews ()
    webHost args { endpoints [ get "/" (homeHandler views) ] }
    0
