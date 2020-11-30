module Scriban.Program

open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.FSharp.Reflection
open System.IO
open System.Reflection
open Scriban
open Scriban.Runtime
open FSharp.Control.Tasks

// ------------
// Define our views
// ------------
type View = | Home

// ------------
// Load view files from disk
// ------------
let readViews (dir : string) =
    typeof<View>
    |> FSharpType.GetUnionCases
    |> Array.map (fun view -> view.Name, Path.Join(dir, $"{view.Name}.html"))
    |> Map.ofArray

// ------------
// Render view with model using Scriban
// ------------
let renderScriban (view : string) (model : 'a) =
    let template = Template.Parse view
    let scriptObject = ScriptObject()
    box model |> scriptObject.Import

    let context = TemplateContext()

    context.PushGlobal scriptObject
    template.RenderAsync context

let handleRenderScriban(views : Map<string, string>) (viewName : string) (model : 'a) : HttpHandler =
    fun ctx -> task {
        let view = views |> Map.find viewName
        let! html = renderScriban view model
        return! Response.ofHtmlString html ctx
    }

// ------------
// Handlers
// ------------
let homeHandler views: HttpHandler =
    let viewName = nameof(Home)
    let queryMap (q: QueryCollectionReader) = {| Name = q.Get "name" "World" |}
    Request.mapQuery queryMap (handleRenderScriban views viewName)

[<EntryPoint>]
let main args =
    let viewsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Views")         

    let views = readViews viewsDirectory
    
    webHost args { 
        endpoints [ get "/" (homeHandler views) ] 
    }
    0
