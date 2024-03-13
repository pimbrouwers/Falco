namespace Falco

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Routing
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

module Hosting = 
    type IEndpointRouteBuilder with
        /// Activates Falco Endpoint integration.
        member x.UseFalcoEndpoints(endpoints : HttpEndpoint seq) =
            let dataSource = FalcoEndpointDatasource(endpoints)
            x.DataSources.Add(dataSource)

    type IApplicationBuilder with
        /// Activates Falco integration with IEndpointRouteBuilder.
        member x.UseFalco(endpoints : HttpEndpoint seq) =
            x.UseRouting()
             .UseEndpoints(fun r -> r.UseFalcoEndpoints(endpoints))

        /// Registers a Falco HttpHandler as exception handler lambda.
        /// See: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?#exception-handler-lambda
        member x.UseFalcoExceptionHandler(exceptionHandler : HttpHandler) =
            let configure (appBuilder : IApplicationBuilder) =
                appBuilder.Run(HttpHandler.toRequestDelegate exceptionHandler)

            x.UseExceptionHandler(configure)

    type WebApplication with
        member x.UseFalco(endpoints : HttpEndpoint seq) =
            (x :> IApplicationBuilder).UseFalco(endpoints) |> ignore
            x

    type FalcoExtensions =
        static member UseFalcoExceptionHandler
            (exceptionHandler : HttpHandler)
            (app : IApplicationBuilder) =
            app.UseFalcoExceptionHandler exceptionHandler

open Hosting

type FalcoAppConfig =
    { Endpoints : HttpEndpoint seq
      Middleware : IApplicationBuilder -> IApplicationBuilder 
      TerminalHandler : HttpHandler }

type Falco(bldr : WebApplicationBuilder) =
    new (args : string array) =
        Falco(WebApplication.CreateBuilder(args))

    new () = Falco([||])

    member internal _.Builder = bldr

    member val Config = { 
        Endpoints = []
        Middleware = id
        TerminalHandler = Response.withStatusCode 404 >> Response.ofEmpty } with get, set

    static member run (app : Falco) =
        // app.Builder.Services
        // |> app.Config.Services
        // |> ignore

        let wapp = app.Builder.Build()

        wapp
        :> IApplicationBuilder
        |> fun x -> x.UseFalco(app.Config.Endpoints)
        |> app.Config.Middleware
        |> fun x -> x.Run(app.Config.TerminalHandler)
        |> ignore

        wapp.Run()

    static member logging (fn : ILoggingBuilder -> ILoggingBuilder) (app : Falco) =
        fn app.Builder.Logging |> ignore 
        app

    static member addService (fn : IServiceCollection -> IServiceCollection) (app : Falco) =
        fn app.Builder.Services |> ignore
        // app.Config <- { app.Config with Services = app.Config.Services >> fn }
        app

    static member addScoped<'a, 'b when 'a : not struct and 'b : not struct> (app : Falco) = 
        Falco.addService (fun svc ->
            svc.AddScoped(serviceType = typeof<'a>, implementationType = typeof<'b>)) app

    static member addSingleton<'a, 'b when 'a : not struct and 'b : not struct> (app : Falco) = 
        Falco.addService (fun svc ->
            svc.AddSingleton(serviceType = typeof<'a>, implementationType = typeof<'b>)) app

    static member addTransient<'a, 'b when 'a : not struct and 'b : not struct> (app : Falco) = 
        Falco.addService (fun svc ->
            svc.AddSingleton(serviceType = typeof<'a>, implementationType = typeof<'b>)) app

    static member addScoped'<'a when 'a : not struct> (fac : IConfiguration -> IServiceProvider -> 'a) (app : Falco) = 
        Falco.addService (fun svc -> svc.AddScoped<'a>(Func<IServiceProvider, 'a>(fac app.Builder.Configuration))) app

    static member addSingleton'<'a when 'a : not struct> (fac : IConfiguration -> IServiceProvider -> 'a) (app : Falco) = 
        Falco.addService (fun svc -> svc.AddSingleton<'a>(Func<IServiceProvider, 'a>(fac app.Builder.Configuration))) app

    static member addTransient'<'a when 'a : not struct> (fac : IConfiguration -> IServiceProvider -> 'a) (app : Falco) = 
        Falco.addService (fun svc -> svc.AddTransient<'a>(Func<IServiceProvider, 'a>(fac app.Builder.Configuration))) app

    static member middleware (fn : IApplicationBuilder -> IApplicationBuilder) (app : Falco) =
        app.Config <- { app.Config with Middleware = app.Config.Middleware >> fn }
        app

    static member plug<'a> (handler : 'a -> HttpHandler) =
        let next' : HttpHandler = fun ctx ->
            let a = ctx.RequestServices.GetRequiredService<'a>()
            handler a ctx

        next'

    static member plug<'a, 'b> (handler : 'a -> 'b -> HttpHandler) =
        let next' : HttpHandler = fun ctx ->
            let a = ctx.RequestServices.GetRequiredService<'a>()
            let b = ctx.RequestServices.GetRequiredService<'b>()
            handler a b ctx

        next'

    static member plug<'a, 'b, 'c> (handler : 'a -> 'b -> 'c -> HttpHandler) =
        let next' : HttpHandler = fun ctx ->
            let a = ctx.RequestServices.GetRequiredService<'a>()
            let b = ctx.RequestServices.GetRequiredService<'b>()
            let c = ctx.RequestServices.GetRequiredService<'c>()
            handler a b c ctx

        next'

    static member all (pattern : string) (handlers : (HttpVerb * HttpHandler) seq) (app : Falco) =
        app.Config <- { app.Config with Endpoints =  Seq.append (Seq.singleton (Routing.all pattern handlers)) app.Config.Endpoints }
        app

    static member route (verb : HttpVerb) (pattern : string) (handler : HttpHandler) (app : Falco) = Falco.all pattern [verb, handler] app

    static member any (pattern : string) (handler : HttpHandler) (app : Falco) = Falco.route ANY pattern handler app

    static member get (pattern : string) (handler : HttpHandler) (app : Falco) = Falco.route GET pattern handler app

    static member head (pattern : string) (handler : HttpHandler) (app : Falco) = Falco.route HEAD pattern handler app

    static member post (pattern : string) (handler : HttpHandler) (app : Falco) = Falco.route POST pattern handler app

    static member put (pattern : string) (handler : HttpHandler) (app : Falco) = Falco.route PUT pattern handler app

    static member patch (pattern : string) (handler : HttpHandler) (app : Falco) = Falco.route PATCH pattern handler app

    static member delete (pattern : string) (handler : HttpHandler) (app : Falco) = Falco.route DELETE pattern handler app

    static member options (pattern : string) (handler : HttpHandler) (app : Falco) = Falco.route OPTIONS pattern handler app

    static member trace (pattern : string) (handler : HttpHandler) (app : Falco) = Falco.route TRACE pattern handler app

    static member endpoints (endpoints : HttpEndpoint seq) (app : Falco) =
        app.Config <- { app.Config with Endpoints =  Seq.append endpoints app.Config.Endpoints }
        app

    static member notFound (handler : HttpHandler) (app : Falco)=
        app.Config <- { app.Config with TerminalHandler = handler }
        app