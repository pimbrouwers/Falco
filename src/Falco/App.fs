namespace Falco

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

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
        let wapp = app.Builder.Build()
        wapp
        :> IApplicationBuilder
        |> app.Config.Middleware
        |> _.Run(app.Config.TerminalHandler)
        |> ignore

        wapp.Run()

    static member logging (fn : ILoggingBuilder -> ILoggingBuilder) (app : Falco) =
        fn app.Builder.Logging |> ignore
        app

    static member configuration (fn : IConfigurationBuilder -> IConfigurationBuilder) (app : Falco) =
        fn app.Builder.Configuration |> ignore
        app

    static member configureServices (fn : IConfiguration -> IServiceCollection -> IServiceCollection) (app : Falco) =
        (fn app.Builder.Configuration) app.Builder.Services |> ignore
        app

    static member configureMiddleware (fn : IApplicationBuilder -> IApplicationBuilder) (app : Falco) =
        app.Config <- { app.Config with Middleware = app.Config.Middleware >> fn }
        app

    static member endpoints (endpoints : HttpEndpoint seq) (app : Falco) =
        app.Config <- { app.Config with Endpoints = Seq.append endpoints app.Config.Endpoints }
        Falco.configureMiddleware (fun x -> x.UseFalco(app.Config.Endpoints)) app

    static member private route (verb : HttpVerb) (pattern : string) (handler : HttpHandler) (app : Falco) =
        app.Config <- {
            app.Config with Endpoints = Seq.append [ Routing.all pattern [verb,handler ] ] app.Config.Endpoints }
        app

    static member any (pattern : string) (handler : HttpHandler) (app : Falco) =
        Falco.route ANY pattern handler app

    static member get (pattern : string) (handler : HttpHandler) (app : Falco) =
        Falco.route GET pattern handler app

    static member head (pattern : string) (handler : HttpHandler) (app : Falco) =
        Falco.route HEAD pattern handler app

    static member post (pattern : string) (handler : HttpHandler) (app : Falco) =
        Falco.route POST pattern handler app

    static member put (pattern : string) (handler : HttpHandler) (app : Falco) =
        Falco.route PUT pattern handler app

    static member patch (pattern : string) (handler : HttpHandler) (app : Falco) =
        Falco.route PATCH pattern handler app

    static member delete (pattern : string) (handler : HttpHandler) (app : Falco) =
        Falco.route DELETE pattern handler app

    static member options (pattern : string) (handler : HttpHandler) (app : Falco) =
        Falco.route OPTIONS pattern handler app

    static member trace (pattern : string) (handler : HttpHandler) (app : Falco) =
        Falco.route TRACE pattern handler app

    static member notFound (handler : HttpHandler) (app : Falco)=
        app.Config <- { app.Config with TerminalHandler = handler }
        app

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
    type Services =
        // static member add (fn : IServiceCollection -> IServiceCollection) (app : Falco) =
        //     fn app.Builder.Services |> ignore
        //     app

        // static member addIf (predicate : bool) (fn : IServiceCollection -> IServiceCollection) (app : Falco) =
        //     if predicate then Services.add fn app
        //     else app

        static member add (fn : IConfiguration -> IServiceCollection -> IServiceCollection) (app : Falco) =
            (fn app.Builder.Configuration) app.Builder.Services |> ignore
            app

        static member addIf (predicate : bool) (fn : IConfiguration -> IServiceCollection -> IServiceCollection) (app : Falco) =
            if predicate then Services.add fn app
            else app

        static member addScopedIf<'a, 'b when 'a : not struct and 'b : not struct> (predicate : bool) (app : Falco) =
            Services.addIf predicate (fun _ svc -> svc.AddScoped(serviceType = typeof<'a>, implementationType = typeof<'b>)) app

        static member addSingletonIf<'a, 'b when 'a : not struct and 'b : not struct> (predicate : bool) (app : Falco) =
            Services.addIf predicate (fun _ svc -> svc.AddSingleton(serviceType = typeof<'a>, implementationType = typeof<'b>)) app

        static member addTransientIf<'a, 'b when 'a : not struct and 'b : not struct> (predicate : bool) (app : Falco) =
            Services.addIf predicate (fun _ svc -> svc.AddSingleton(serviceType = typeof<'a>, implementationType = typeof<'b>)) app

        static member addScoped<'a, 'b when 'a : not struct and 'b : not struct> (app : Falco) =
            Services.addScopedIf<'a, 'b> true app

        static member addSingleton<'a, 'b when 'a : not struct and 'b : not struct> (app : Falco) =
            Services.addSingletonIf<'a, 'b> true app

        static member addTransient<'a, 'b when 'a : not struct and 'b : not struct> (app : Falco) =
            Services.addTransientIf<'a, 'b> true app

        static member addScopedTypeIf<'a when 'a : not struct> (predicate : bool) (app : Falco) =
            Services.addIf predicate (fun _ svc -> svc.AddScoped<'a>()) app

        static member addSingletonTypeIf<'a when 'a : not struct> (predicate : bool) (app : Falco) =
            Services.addIf predicate (fun _ svc -> svc.AddSingleton<'a>()) app

        static member addTransientTypeIf<'a when 'a : not struct> (predicate : bool) (app : Falco) =
            Services.addIf predicate (fun _ svc -> svc.AddSingleton<'a>()) app

        static member addScopedType<'a when 'a : not struct> (app : Falco) =
            Services.addScopedTypeIf<'a> true app

        static member addSingletonType<'a when 'a : not struct> (app : Falco) =
            Services.addSingletonTypeIf<'a> true app

        static member addTransientType<'a when 'a : not struct> (app : Falco) =
            Services.addTransientTypeIf<'a> true app

        static member addInstanceIf<'a when 'a : not struct> (predicate : bool) (fn : IConfiguration -> 'a) (app : Falco) =
            Services.addIf predicate (fun _ svc -> svc.AddSingleton<'a>(implementationInstance = fn app.Builder.Configuration)) app

        static member addInstance<'a when 'a : not struct> (fn : IConfiguration -> 'a) (app : Falco) =
            Services.addInstanceIf true fn app

    type Middleware =
        static member add (fn : IApplicationBuilder -> IApplicationBuilder) (app : Falco) =
            Falco.configureMiddleware fn app

        static member addIf (predicate : bool) (fn : IApplicationBuilder -> IApplicationBuilder) (app : Falco) =
            if predicate then Middleware.add fn app
            else app

        static member addType<'a> (app : Falco) =
            let fn (app : IApplicationBuilder) = app.UseMiddleware<'a>()
            Middleware.add fn app

        static member addTypeIf<'a> (predicate : bool) (app : Falco) =
            if predicate then Middleware.addType<'a> app
            else app
