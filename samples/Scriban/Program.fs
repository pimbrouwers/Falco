namespace Falco.Scriban

open System.IO
open System.Threading.Tasks
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Scriban
open Scriban.Runtime

type IViewEngine =
    abstract member RenderAsync : view : string * model : 'a -> ValueTask<string>

type ScribanViewEngine (views : Map<string, Template>) =
    interface IViewEngine with
        member _.RenderAsync(view : string, model : 'a) =
            match Map.tryFind view views with
            | Some template -> template.RenderAsync(model)
            | None -> failwithf "View '%s' was not found" view

module Response =
    let renderScriban
        (viewEngine : IViewEngine)
        (view : string)
        (model : 'a) : HttpHandler = fun ctx ->
        task {
            let! html = viewEngine.RenderAsync(view, model)
            return Response.ofHtmlString html ctx
        }

module Middleware =
    let withViewEngine (next : IViewEngine -> HttpHandler) : HttpHandler = fun ctx ->
        let viewEngine = ctx.RequestServices.GetRequiredService<IViewEngine>()
        next viewEngine ctx

module Handlers =
    open Middleware

    let exceptionHandler : HttpHandler =
        Response.withStatusCode 500 >> Response.ofPlainText "Server error"

    let homepage : HttpHandler =
        withViewEngine (fun viewEngine ->
            let queryMap (q: QueryCollectionReader) =
                {|
                    Name = q.Get "name" "World"
                |}

            let next =
                Response.renderScriban viewEngine "Home"

            Request.mapQuery queryMap next)

module Program =
    [<EntryPoint>]
    let main args =
        // Load & parse views on startup
        let views =
            let viewsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Views")

            Directory.EnumerateFiles(viewsDirectory)
            |> Seq.map (fun file ->
                let viewName = Path.GetFileNameWithoutExtension(file)
                let viewContent = File.ReadAllText(file)
                let view = Template.Parse(viewContent)
                viewName, view)
            |> Map.ofSeq

        // Register viewEngine with service collection
        let scribanService (svc : IServiceCollection) =
            svc.AddScoped<IViewEngine, ScribanViewEngine>(fun _ ->
                new ScribanViewEngine(views))

        webHost args {
            add_service scribanService

            use_https
            use_compression
            use_if    FalcoExtensions.IsDevelopment DeveloperExceptionPageExtensions.UseDeveloperExceptionPage
            use_ifnot FalcoExtensions.IsDevelopment (FalcoExtensions.UseFalcoExceptionHandler Handlers.exceptionHandler)

            endpoints [
                get "/" Handlers.homepage
            ]
        }
        0
