namespace Falco

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

type FalcoAppConfig =
    { Endpoints : HttpEndpoint seq
      Middleware : IApplicationBuilder -> IApplicationBuilder
      TerminalHandler : HttpHandler }

type Falco private (bldr : WebApplicationBuilder) =
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

    static member newApp (bldr : WebApplicationBuilder) = Falco(bldr)

    static member newApp (options : WebApplicationOptions) = Falco(WebApplication.CreateBuilder(options))

    static member newApp (args : string array) = Falco(WebApplication.CreateBuilder(args))

    static member newApp () = Falco.newApp [||]

    static member addConfiguration (fn : IConfigurationBuilder -> IConfigurationBuilder) (app : Falco) =
        fn app.Builder.Configuration |> ignore
        app

    static member addLogging (fn : ILoggingBuilder -> ILoggingBuilder) (app : Falco) =
        fn app.Builder.Logging |> ignore
        app

    static member addServices (fn : IConfiguration -> IServiceCollection -> IServiceCollection) (app : Falco) =
        (fn app.Builder.Configuration) app.Builder.Services |> ignore
        app

    static member addMiddleware (fn : IApplicationBuilder -> IApplicationBuilder) (app : Falco) =
        app.Config <- { app.Config with Middleware = app.Config.Middleware >> fn }
        app

    static member endpoints (endpoints : HttpEndpoint seq) (app : Falco) =
        app.Config <- { app.Config with Endpoints = Seq.append endpoints app.Config.Endpoints }
        Falco.addMiddleware (fun x -> x.UseFalco(app.Config.Endpoints)) app

    static member private route (verb : HttpVerb) (pattern : string) (handler : HttpHandler) (app : Falco) =
        Falco.endpoints [ Routing.all pattern [ verb,handler ] ] app

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

    static member plug<'T> handler : HttpHandler = fun ctx ->
        let a = ctx.RequestServices.GetRequiredService<'T>()
        handler a ctx

    static member plug<'T1, 'T2> handler : HttpHandler = fun ctx ->
        let b = ctx.RequestServices.GetRequiredService<'T2>()
        Falco.plug<'T1> (fun a -> handler a b) ctx

    static member plug<'T1, 'T2, 'T3> handler : HttpHandler = fun ctx ->
        let c = ctx.RequestServices.GetRequiredService<'T3>()
        Falco.plug<'T1, 'T2> (fun a b -> handler a b c) ctx

    static member plug<'T1, 'T2, 'T3, 'T4> handler : HttpHandler = fun ctx ->
        let d = ctx.RequestServices.GetRequiredService<'T4>()
        Falco.plug<'T1, 'T2, 'T3> (fun a b c -> handler a b c d) ctx

    static member plug<'T1, 'T2, 'T3, 'T4, 'T5> handler : HttpHandler = fun ctx ->
        let e = ctx.RequestServices.GetRequiredService<'T5>()
        Falco.plug<'T1, 'T2, 'T3, 'T4> (fun a b c d -> handler a b c d e) ctx

[<RequireQualifiedAccess>]
module Falco =
    type Services =
        static member add (fn : IConfiguration -> IServiceCollection -> IServiceCollection) (app : Falco) =
            (fn app.Builder.Configuration) app.Builder.Services |> ignore
            app

        static member addIf (predicate : bool) (fn : IConfiguration -> IServiceCollection -> IServiceCollection) (app : Falco) =
            if predicate then Services.add fn app
            else app

        static member addStaticIf (predicate) (fn : IServiceCollection -> IServiceCollection) (app : Falco) =
            Services.addIf predicate (fun _ svc -> fn svc) app

        static member addStatic (fn : IServiceCollection -> IServiceCollection) (app : Falco) =
            Services.addStaticIf true fn app

        static member addInstanceIf<'T when 'T : not struct> (predicate : bool) (fn : IConfiguration -> 'T) (app : Falco) =
            Services.addIf predicate (fun _ svc -> svc.AddSingleton<'T>(implementationInstance = fn app.Builder.Configuration)) app

        static member addInstance<'T when 'T : not struct> (fn : IConfiguration -> 'T) (app : Falco) =
            Services.addInstanceIf true fn app

        static member addScopedIf<'T, 'U when 'T : not struct and 'U : not struct> (predicate : bool) (app : Falco) =
            Services.addIf predicate (fun _ svc -> svc.AddScoped(serviceType = typeof<'T>, implementationType = typeof<'U>)) app

        static member addScoped<'T, 'U when 'T : not struct and 'U : not struct> (app : Falco) =
            Services.addScopedIf<'T, 'U> true app

        static member addScopedTypeIf<'T when 'T : not struct> (predicate : bool) (app : Falco) =
            Services.addIf predicate (fun _ svc -> svc.AddScoped<'T>()) app

        static member addScopedType<'T when 'T : not struct> (app : Falco) =
            Services.addScopedTypeIf<'T> true app

        static member addSingletonIf<'T, 'U when 'T : not struct and 'U : not struct> (predicate : bool) (app : Falco) =
            Services.addIf predicate (fun _ svc -> svc.AddSingleton(serviceType = typeof<'T>, implementationType = typeof<'U>)) app

        static member addSingleton<'T, 'U when 'T : not struct and 'U : not struct> (app : Falco) =
            Services.addSingletonIf<'T, 'U> true app

        static member addSingletonTypeIf<'T when 'T : not struct> (predicate : bool) (app : Falco) =
            Services.addIf predicate (fun _ svc -> svc.AddSingleton<'T>()) app

        static member addSingletonType<'T when 'T : not struct> (app : Falco) =
            Services.addSingletonTypeIf<'T> true app

        static member addTransientIf<'T, 'U when 'T : not struct and 'U : not struct> (predicate : bool) (app : Falco) =
            Services.addIf predicate (fun _ svc -> svc.AddSingleton(serviceType = typeof<'T>, implementationType = typeof<'U>)) app

        static member addTransient<'T, 'U when 'T : not struct and 'U : not struct> (app : Falco) =
            Services.addTransientIf<'T, 'U> true app

        static member addTransientTypeIf<'T when 'T : not struct> (predicate : bool) (app : Falco) =
            Services.addIf predicate (fun _ svc -> svc.AddSingleton<'T>()) app

        static member addTransientType<'T when 'T : not struct> (app : Falco) =
            Services.addTransientTypeIf<'T> true app

    type Middleware =
        static member add (fn : IApplicationBuilder -> IApplicationBuilder) (app : Falco) =
            Falco.addMiddleware fn app

        static member addIf (predicate : bool) (fn : IApplicationBuilder -> IApplicationBuilder) (app : Falco) =
            if predicate then Middleware.add fn app
            else app

        static member addType<'T> (app : Falco) =
            let fn (app : IApplicationBuilder) = app.UseMiddleware<'T>()
            Middleware.add fn app

        static member addTypeIf<'T> (predicate : bool) (app : Falco) =
            if predicate then Middleware.addType<'T> app
            else app