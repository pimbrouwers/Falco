module Falco.Scriban.Program

open System
open System.Collections.Generic
open System.IO
open System.Threading.Tasks
open Falco
open Falco.Routing
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Scriban

type ITemplateSerice =
    abstract member Render : name: string * model: obj -> ValueTask<string>

type ScribanTemplateService(templates : IDictionary<string, Template>) = 
    interface ITemplateSerice with 
        member _.Render(name, model) = 
            let found, template = templates.TryGetValue(name)
            if found then template.RenderAsync(model)
            else failwithf "Template '%s' was not found" name

module PageController =
    let homepage (templateServce : ITemplateSerice) : HttpHandler =
        let queryMap (q: QueryCollectionReader) =
            {| Name = q.Get("name", "World") |}

        let next model : HttpHandler = fun ctx ->
            task {
                let! html = templateServce.Render("Home", model)
                return Response.ofHtmlString html ctx
            }

        Request.mapQuery queryMap next

type App(templateService : ITemplateSerice) = 
    member _.Endpoints = 
        seq { 
            get "/" (PageController.homepage templateService)
        }

    member _.NotFound = 
        Response.withStatusCode 404 
        >> Response.ofPlainText "Not Found"

[<EntryPoint>]
let main args =
    let executionDirectory = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0])
    
    let bldr = WebApplication.CreateBuilder(args)
    
    let app =
        let scribanTemplates = 
            Directory.EnumerateFiles(Path.Combine(executionDirectory, "Views"))
            |> Seq.map (fun file ->
                let viewName = Path.GetFileNameWithoutExtension(file)
                let viewContent = File.ReadAllText(file)
                let view = Template.Parse(viewContent)
                viewName, view)
            |> Map.ofSeq
        let templateService = ScribanTemplateService(scribanTemplates) 
        App(templateService)
        
    let wapp = bldr.Build()
    
    wapp.UseFalco(app.Endpoints)
        .Run(app.NotFound) 
        |> ignore
    
    wapp.Run()
    0 // Exit code
