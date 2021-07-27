module Scriban.Program

open System.IO
open System.Threading.Tasks
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.FSharp.Reflection
open Scriban
open Scriban.Runtime

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

let handleRenderScriban(template : Template) (model : 'a) : HttpHandler = fun ctx ->
    let scribanTask = renderScriban template model

    if scribanTask.IsCompletedSuccessfully then         
        let continuation (htmlTask : Task<string>) : Task = Response.ofHtmlString htmlTask.Result ctx
        let renderTask = scribanTask.AsTask().ContinueWith(continuation, TaskContinuationOptions.OnlyOnRanToCompletion)
        renderTask.Wait ()
        renderTask.Result
    else 
        Task.CompletedTask
    
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

let textHandler : HttpHandler =
    let getMessage (query : QueryCollectionReader) =
        query.GetString "name" "World" 
        |> sprintf "Hello %s"
        
    Request.mapQuery getMessage Response.ofPlainText

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
