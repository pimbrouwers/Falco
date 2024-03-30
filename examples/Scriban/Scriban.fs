module Falco.Scriban.Program

open System
open System.IO
open System.Threading.Tasks
open Falco
open Microsoft.AspNetCore.Builder
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
        |> Map.ofList

    interface ITemplates with
        member _.Render(name, model) =
            match Map.tryFind name templates with
            | Some template ->
                let tmpl = Template.Parse template
                tmpl.RenderAsync(model)
            | None -> failwithf "Template '%s' was not found" name

module Pages =
    let private renderPage name viewModel : HttpHandler = fun ctx ->
        let templateService = ctx.Plug<ITemplates>() // <-- obtain our template service from the dependency container
        task {
            let! html = templateService.Render(name, viewModel) // <-- render our template with the provided view model as string literal
            return Response.ofHtmlString html ctx // <-- return template literal as "text/html; charset=utf-8" response 
        }

    let homepage : HttpHandler = fun ctx ->
        let query = Request.getQuery ctx // <-- obtain access to strongly-typed representation of the query string
        let viewModel = {| Name = query?name.AsStringNonEmpty("World") |} // <-- access 'name' from query, or default to 'World'
        renderPage "Home" viewModel ctx

    let notFound : HttpHandler =
        renderPage "404" ""

[<EntryPoint>]
let main args =
    // resolve our views directory
    let viewsDirectory =
        let allArgs = Environment.GetCommandLineArgs() // <-- conveniently gives us the full execution path
        match Seq.tryHead allArgs with
        | Some executionPath ->
            let executionDirectory = Path.GetDirectoryName executionPath
            Path.Combine (executionDirectory, "Views")
        | None ->
            failwith "Could not access the full command line arguments, for execution path"

    let bldr = WebApplication.CreateBuilder(args)

    bldr.Services
        .AddSingleton<ITemplates>(ScribanTemplates(viewsDirectory)) // <-- register ITemplates implementation as a dependency 
        |> ignore

    bldr.Build()
        .UseFalco()
        .FalcoGet("/", Pages.homepage)
        .FalcoNotFound(Pages.notFound)
        .Run()
        
    0 // Exit code
