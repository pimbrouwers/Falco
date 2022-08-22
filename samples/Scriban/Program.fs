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

type IViewRenderer =
    abstract member RenderAsync : view : string * 'a -> ValueTask<string>

type ScribanRenderer (views : Map<string, Template>) =
    interface IViewRenderer with
        member _.RenderAsync(view : string, model : 'a) =
            match Map.tryFind view views with
            | Some template -> template.RenderAsync(model)
            | None -> failwithf "View '%s' was not found" view

module Response =
    let renderScriban
        (renderer : IViewRenderer)
        (view : string)
        (model : 'a) : HttpHandler = fun ctx ->
        task {
            let! html = renderer.RenderAsync(view, model)
            return Response.ofHtmlString html ctx
        }

module Middleware =
    let withViews (next : IViewRenderer -> HttpHandler) : HttpHandler = fun ctx ->
        let renderer = ctx.RequestServices.GetRequiredService<IViewRenderer>()
        next renderer ctx

module Handlers =
    open Middleware

    let homepage : HttpHandler =
        withViews (fun renderer ->
            let queryMap (q: QueryCollectionReader) =
                {|
                    Name = q.Get "name" "World"
                |}

            let next =
                Response.renderScriban renderer "Home"

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

        // Register renderer with service collection
        let scribanService (svc : IServiceCollection) =
            svc.AddScoped<IViewRenderer, ScribanRenderer>(fun _ ->
                new ScribanRenderer(views))

        webHost args {
            add_service scribanService

            use_https
            use_compression
            use_middleware  DeveloperExceptionPageExtensions.UseDeveloperExceptionPage

            endpoints [
                get "/" Handlers.homepage
            ]
        }
        0
