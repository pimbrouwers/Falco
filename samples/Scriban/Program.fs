module Falco.Scriban.Program

open System.IO
open System.Threading.Tasks
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.Extensions.DependencyInjection
open Scriban

// ------------
// View Engine
// ------------
type IViewEngine =
    abstract member RenderAsync : view : string * model : 'a -> ValueTask<string>

type ScribanViewEngine (views : Map<string, Template>) =
    interface IViewEngine with
        member _.RenderAsync(view : string, model : 'a) =
            match Map.tryFind view views with
            | Some template -> template.RenderAsync(model)
            | None -> failwithf "View '%s' was not found" view

// ------------
// Response helper func for IViewEngine
// ------------
module Response =
    let renderViewEngine
        (viewEngine : IViewEngine)
        (view : string)
        (model : 'a) : HttpHandler = fun ctx ->
        task {
            let! html = viewEngine.RenderAsync(view, model)
            return Response.ofHtmlString html ctx
        }

// ------------
// App Middleware
// ------------
module Middleware =
    let withViewEngine (next : IViewEngine -> HttpHandler) : HttpHandler = fun ctx ->
        let viewEngine = ctx.RequestServices.GetRequiredService<IViewEngine>()
        next viewEngine ctx

// ------------
// Pages
// ------------
module Pages =
    open Middleware

    let homepage : HttpHandler =
        withViewEngine (fun viewEngine ->
            let queryMap (q: QueryCollectionReader) =
                {| Name = q.Get "name" "World" |}

            let next =
                Response.renderViewEngine viewEngine "Home"

            Request.mapQuery queryMap next)

// ------------
// Scriban templates
// ------------
let loadScribanTemplates (root : string) =
    let viewsDirectory = Path.Combine(root, "Views")

    Directory.EnumerateFiles(viewsDirectory)
    |> Seq.map (fun file ->
        let viewName = Path.GetFileNameWithoutExtension(file)
        let viewContent = File.ReadAllText(file)
        let view = Template.Parse(viewContent)
        viewName, view)
    |> Map.ofSeq

let templates = loadScribanTemplates (Directory.GetCurrentDirectory())

// ------------
// Register services
// ------------
let scribanService (svc : IServiceCollection) =
    svc.AddScoped<IViewEngine, ScribanViewEngine>(fun _ ->
        new ScribanViewEngine(templates))

webHost [||] {
    use_https

    add_service scribanService

    endpoints [
        get "/" Pages.homepage
    ]
}
