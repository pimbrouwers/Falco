module Scriban.Program

open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.FSharp.Reflection
open System.IO
open Scriban
open Scriban.Runtime
open FSharp.Control.Tasks

// ------------
// Define our views
// ------------
type View = | Home

// ------------
// Render view with model using Scriban
// ------------
let renderScriban (template : Template) (model : 'a) =     
    let scriptObject = ScriptObject()
    box model |> scriptObject.Import

    let context = TemplateContext()

    context.PushGlobal scriptObject
    template.RenderAsync context

let handleRenderScriban(template : Template) (model : 'a) : HttpHandler =
    fun ctx -> task {        
        let! html = renderScriban template model
        return! Response.ofHtmlString html ctx
    }

// ------------
// Handlers
// ------------
let homeHandler views : HttpHandler =
    let view = views |> Map.find (nameof(Home))
    let queryMap (q: QueryCollectionReader) = 
        {| 
            Name = q.Get "name" "World" 
        |}
    let next = handleRenderScriban view
    Request.mapQuery queryMap next 

// ------------
// Load view files from disk
// ------------
let readViews (dir : string) =
    let readView (viewInfo : UnionCaseInfo) = 
        let viewPath = Path.Join(dir, $"{viewInfo.Name}.html")
        let viewText = File.ReadAllText(viewPath)
        let view = Template.Parse viewText

        viewInfo.Name, view

    typeof<View>
    |> FSharpType.GetUnionCases
    |> Array.map readView
    |> Map.ofArray

[<EntryPoint>]
let main args =
    // Load & parse views on startup
    let viewsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Views")         
    let views = readViews viewsDirectory
    
    webHost args { 
        endpoints [ 
                    get "/" (homeHandler views) 
                  ] 
    }
    0
