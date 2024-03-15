namespace Falco

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

[<AutoOpen>]
module Hosting =
    type HttpContext with
        /// Attempts to obtain dependency from IServiceCollection
        /// Throws InvalidDependencyException on null.
        member x.GetService<'T>() =
            x.RequestServices.GetRequiredService<'T>()

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

    static member plug<'a> handler : HttpHandler = fun ctx ->
        let a = ctx.RequestServices.GetRequiredService<'a>()
        handler a ctx 

    static member plug<'a, 'b> handler : HttpHandler = fun ctx ->
        let b = ctx.RequestServices.GetRequiredService<'b>()
        Falco.plug<'a> (fun a -> handler a b) ctx

    static member plug<'a, 'b, 'c> handler : HttpHandler = fun ctx ->
        let c = ctx.RequestServices.GetRequiredService<'c>()
        Falco.plug<'a, 'b> (fun a b -> handler a b c) ctx

    static member plug<'a, 'b, 'c, 'd> handler : HttpHandler = fun ctx ->
        let d = ctx.RequestServices.GetRequiredService<'d>()
        Falco.plug<'a, 'b, 'c> (fun a b c -> handler a b c d) ctx


    static member plug<'a, 'b, 'c, 'd, 'e> handler : HttpHandler = fun ctx ->
        let e = ctx.RequestServices.GetRequiredService<'d>()
        Falco.plug<'a, 'b, 'c, 'd> (fun a b c d -> handler a b c d e) ctx

[<RequireQualifiedAccess>]
module Falco = 
    let run (app : Falco) =
        let wapp = app.Builder.Build()

        wapp
        :> IApplicationBuilder
        |> fun x -> x.UseFalco(app.Config.Endpoints)
        |> app.Config.Middleware
        |> fun x -> x.Run(app.Config.TerminalHandler)
        |> ignore

        wapp.Run()

    let logging (fn : ILoggingBuilder -> ILoggingBuilder) (app : Falco) =
        fn app.Builder.Logging |> ignore
        app

    let configuration (fn : IConfigurationBuilder -> IConfigurationBuilder) (app : Falco) =
        fn app.Builder.Configuration |> ignore
        app

    let notFound (handler : HttpHandler) (app : Falco)=
        app.Config <- { app.Config with TerminalHandler = handler }
        app

    let endpoints (endpoints : HttpEndpoint seq) (app : Falco) =
        app.Config <- { app.Config with Endpoints =  Seq.append endpoints app.Config.Endpoints }
        app

    let all (pattern : string) (handlers : (HttpVerb * HttpHandler) seq) (app : Falco) =
        app.Config <- { app.Config with Endpoints =  Seq.append (Seq.singleton (Routing.all pattern handlers)) app.Config.Endpoints }
        app

    let route (verb : HttpVerb) (pattern : string) (handler : HttpHandler) (app : Falco) = 
        all pattern [verb, handler] app

    let any (pattern : string) (handler : HttpHandler) (app : Falco) = 
        route ANY pattern handler app

    let get (pattern : string) (handler : HttpHandler) (app : Falco) = 
        route GET pattern handler app

    let head (pattern : string) (handler : HttpHandler) (app : Falco) = 
        route HEAD pattern handler app

    let post (pattern : string) (handler : HttpHandler) (app : Falco) = 
        route POST pattern handler app

    let put (pattern : string) (handler : HttpHandler) (app : Falco) = 
        route PUT pattern handler app

    let patch (pattern : string) (handler : HttpHandler) (app : Falco) = 
        route PATCH pattern handler app

    let delete (pattern : string) (handler : HttpHandler) (app : Falco) = 
        route DELETE pattern handler app

    let options (pattern : string) (handler : HttpHandler) (app : Falco) = 
        route OPTIONS pattern handler app

    let trace (pattern : string) (handler : HttpHandler) (app : Falco) = 
        route TRACE pattern handler app

    type Services =
        static member add (fn : IServiceCollection -> IServiceCollection) (app : Falco) =
            fn app.Builder.Services |> ignore
            app

        static member addConfigured (fn : IConfiguration -> IServiceCollection -> IServiceCollection) (app : Falco) =
            (fn app.Builder.Configuration) app.Builder.Services |> ignore
            app

        static member addScoped<'a when 'a : not struct> (app : Falco) =
            Services.add (fun svc -> svc.AddScoped(serviceType = typeof<'a>)) app

        static member addSingleton<'a when 'a : not struct> (app : Falco) =
            Services.add (fun svc -> svc.AddSingleton(serviceType = typeof<'a>)) app

        static member addTransient<'a when 'a : not struct> (app : Falco) =
            Services.add (fun svc -> svc.AddTransient(serviceType = typeof<'a>)) app

        static member addScoped<'a, 'b when 'a : not struct and 'b : not struct> (app : Falco) =
            Services.add (fun svc -> svc.AddScoped(serviceType = typeof<'a>, implementationType = typeof<'b>)) app

        static member addSingleton<'a, 'b when 'a : not struct and 'b : not struct> (app : Falco) =
            Services.add (fun svc -> svc.AddSingleton(serviceType = typeof<'a>, implementationType = typeof<'b>)) app

        static member addTransient<'a, 'b when 'a : not struct and 'b : not struct> (app : Falco) =
            Services.add (fun svc -> svc.AddSingleton(serviceType = typeof<'a>, implementationType = typeof<'b>)) app

        static member addScopedConfigured<'a when 'a : not struct> (fac : IConfiguration -> IServiceProvider -> 'a) (app : Falco) =
            Services.addConfigured (fun conf svc -> svc.AddScoped<'a>(Func<IServiceProvider, 'a>(fac conf))) app

        static member addSingletonConfigured<'a when 'a : not struct> (fac : IConfiguration -> IServiceProvider -> 'a) (app : Falco) =
            Services.addConfigured (fun conf svc -> svc.AddSingleton<'a>(Func<IServiceProvider, 'a>(fac conf))) app

        static member addTransientConfigured<'a when 'a : not struct> (fac : IConfiguration -> IServiceProvider -> 'a) (app : Falco) =
            Services.addConfigured (fun conf svc -> svc.AddTransient<'a>(Func<IServiceProvider, 'a>(fac conf))) app

    module Middleware =
        let add (fn : IApplicationBuilder -> IApplicationBuilder) (app : Falco) =
            app.Config <- { app.Config with Middleware = app.Config.Middleware >> fn }
            app

        let addIf (predicate : bool) (fn : IApplicationBuilder -> IApplicationBuilder) (app : Falco) =
            if predicate then add fn app
            else app

        let addInline (requestDelegate : RequestDelegate -> RequestDelegate) (app : Falco) =
            let fn (app : IApplicationBuilder) = app.Use(requestDelegate)
            add fn app

        let addInlineIf (predicate : bool) (requestDelegate : RequestDelegate -> RequestDelegate) (app : Falco) =
            if predicate then addInline requestDelegate app
            else app

        let addType<'a> (app : Falco) =
            let fn (app : IApplicationBuilder) = app.UseMiddleware<'a>()
            add fn app

        let addTypeIf<'a> (predicate : bool) (app : Falco) =
            if predicate then addType<'a> app
            else app

        let addProps<'a> ([<ParamArray>] props : obj array) = fun (app : Falco) ->
            let fn (app : IApplicationBuilder) = app.UseMiddleware<'a>(props)
            add fn app

        let addPropsIf<'a> ([<ParamArray>] props : obj array) = fun (predicate : bool) (app : Falco) ->
            if predicate then addProps<'a> props app
            else app
