module Falco.Scriban.Program

open System
open System.IO
open System.Threading.Tasks
open Falco
open Falco.Routing
open Microsoft.Extensions.Configuration
open Scriban

type ITemplates = 
    abstract member TryGet : string -> string option

type ITemplateService =
    abstract member Render : name: string * model: obj -> ValueTask<string>

module PageController =
    let homepage : HttpHandler =
        Falco.plug<ITemplateService> <| fun templateService ->
        let queryMap (q: QueryCollectionReader) =
            {| Name = q.Get("name", "World") |}

        let next model : HttpHandler = fun ctx ->
            task {
                let! html = templateService.Render("Home", model)
                return Response.ofHtmlString html ctx
            }

        Request.mapQuery queryMap next

module app =
    let endpoints = 
        [ get "/" PageController.homepage ]

    let notFound = 
        Response.withStatusCode 404 
        >> Response.ofPlainText "Not Found"

type ScribanTemplates(templates : (string * string) seq) = 
    interface ITemplates with 
        member _.TryGet(name) = 
            templates
            |> Seq.tryPick (fun (tmplName, tmpl) -> 
                match String.Equals(name, tmplName, StringComparison.OrdinalIgnoreCase) with 
                | true -> Some tmpl
                | _ -> None)

type ScribanTemplateService(templates : ITemplates) = 
    interface ITemplateService with 
        member _.Render(name, model) = 
            match templates.TryGet name with 
            | Some template ->
                let tmpl = Template.Parse template
                tmpl.RenderAsync(model)
            | None -> failwithf "Template '%s' was not found" name

[<EntryPoint>]
let main args =    
    let executionPath = Environment.GetCommandLineArgs()[0]
    let executionDirectory = Path.GetDirectoryName executionPath
    
    let scribanTemplates (conf : IConfiguration) _ = 
        let viewsDirectoryName = conf.GetValue<string>("ViewsDirectory")
        let viewsDirectory = Path.Combine (executionDirectory, viewsDirectoryName)
        ScribanTemplates [ 
            for file in Directory.EnumerateFiles viewsDirectory do 
                let viewName = Path.GetFileNameWithoutExtension file 
                let viewContent = File.ReadAllText file                
                viewName, viewContent 
        ] :> ITemplates

    Falco args
    |> Falco.Services.addSingletonConfigured<ITemplates> scribanTemplates
    |> Falco.Services.addSingleton<ITemplateService, ScribanTemplateService>
    |> Falco.endpoints app.endpoints
    |> Falco.notFound app.notFound
    |> Falco.run 
    
    0 // Exit code
