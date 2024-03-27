namespace FalcoTutorial

module App =
    open Falco
    open Falco.Routing
    open FalcoTutorial.Web

    let endpoints = [
        get Route.index EntryController.index
        get Route.notFound ErrorController.notFound
        all Route.entryCreate [
            GET, EntryController.create
            POST, EntryController.save ]
        all Route.entryEdit [
            GET, EntryController.edit
            POST, EntryController.save ]
    ]

module Program =
    open Falco
    open Microsoft.AspNetCore.Builder
    open Microsoft.Extensions.Configuration
    open Microsoft.Extensions.DependencyInjection
    open FalcoTutorial.Infrastructure

    let dbConnectionContext (conf : IConfiguration) _ =
        { new IDbConnectionContext with
            member _.ConnectionString = conf.GetConnectionString("Default")}

    let notFound =
        Response.withStatusCode 404
        >> Response.ofPlainText "Not Found"

    [<EntryPoint>]
    let main args =
        Falco.newApp args
        |> Falco.Services.addStatic AntiforgeryServiceCollectionExtensions.AddAntiforgery
        |> Falco.Services.addInstance dbConnectionContext
        |> Falco.Services.addSingleton<IDbConnectionFactory, SqliteDbConnectionFactory>
        |> Falco.Middleware.add StaticFileExtensions.UseStaticFiles
        |> Falco.endpoints App.endpoints
        |> Falco.notFound notFound
        |> Falco.run
        0