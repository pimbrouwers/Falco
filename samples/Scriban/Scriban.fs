module Falco.Scriban.Program

open System
open System.IO
open System.Threading.Tasks
open Falco
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Scriban

type ITemplates =
    abstract member Render : name: string * model: obj -> ValueTask<string>

type ScribanTemplates(viewsDirectory : string) =
    do
        if not(Directory.Exists viewsDirectory) then
            failwithf "Directory was not found, '%s'" viewsDirectory

    let templates =
        [ for file in Directory.EnumerateFiles viewsDirectory do
            let viewName = Path.GetFileNameWithoutExtension file
            let viewContent = File.ReadAllText file
            viewName, viewContent ]

    interface ITemplates with
        member _.Render(name, model) =
            match Seq.tryFind (fun (viewName, _) -> viewName = name) templates with
            | Some (_, template) ->
                let tmpl = Template.Parse template
                tmpl.RenderAsync(model)
            | None -> failwithf "Template '%s' was not found" name

module Pages =
    let private renderPage name model : HttpHandler = fun ctx ->
        let templateService = ctx.Plug<ITemplates>()
        task {
            let! html = templateService.Render(name, model)
            return Response.ofHtmlString html ctx
        }

    let homepage : HttpHandler = fun ctx ->
        let queryMap (q: RequestData) =
            {| Name = q.GetString("name", "World") |}

        Request.mapQuery queryMap (renderPage "Home") ctx

    let notFound : HttpHandler =
        renderPage "404" {||}

[<EntryPoint>]
let main args =
    let bldr = WebApplication.CreateBuilder(args)

    let viewsDirectory =
        let executionPath = Environment.GetCommandLineArgs()[0]
        let executionDirectory = Path.GetDirectoryName executionPath
        let viewsDirectoryName = bldr.Configuration.GetValue<string>("ViewsDirectory")
        Path.Combine (executionDirectory, viewsDirectoryName)

    let scribanTemplates : ITemplates = ScribanTemplates(viewsDirectory)

    bldr.Services
        .AddSingleton(scribanTemplates)
        |> ignore

    bldr.Build()
        .UseFalco()
        .FalcoGet("/", Pages.homepage)
        .FalcoNotFound(Pages.notFound)
        .Run()

    0 // Exit code
