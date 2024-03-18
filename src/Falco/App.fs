namespace Falco

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
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
        |> _.UseFalco(app.Config.Endpoints)
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

    static member notFound (handler : HttpHandler) (app : Falco)=
        app.Config <- { app.Config with TerminalHandler = handler }
        app

    static member endpoints (endpoints : HttpEndpoint seq) (app : Falco) =
        app.Config <- { app.Config with Endpoints =  Seq.append endpoints app.Config.Endpoints }
        app

    static member all (pattern : string) (handlers : (HttpVerb * HttpHandler) seq) (app : Falco) =
        app.Config <- { app.Config with Endpoints =  Seq.append (Seq.singleton (Routing.all pattern handlers)) app.Config.Endpoints }
        app

    static member route (verb : HttpVerb) (pattern : string) (handler : HttpHandler) (app : Falco) =
        Falco.all pattern [verb, handler] app

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
        static member add (fn : IServiceCollection -> IServiceCollection) (app : Falco) =
            fn app.Builder.Services |> ignore
            app

        static member addIf (predicate : bool) (fn : IServiceCollection -> IServiceCollection) (app : Falco) =
            if predicate then Services.add fn app
            else app


        static member addConfigured (fn : IConfiguration -> IServiceCollection -> IServiceCollection) (app : Falco) =
            (fn app.Builder.Configuration) app.Builder.Services |> ignore
            app

        static member addConfiguredIf (predicate : bool) (fn : IConfiguration -> IServiceCollection -> IServiceCollection) (app : Falco) =
            if predicate then Services.addConfigured fn app
            else app

        static member addScoped<'a, 'b when 'a : not struct and 'b : not struct> (app : Falco) =
            Services.add (fun svc -> svc.AddScoped(serviceType = typeof<'a>, implementationType = typeof<'b>)) app

        static member addSingleton<'a, 'b when 'a : not struct and 'b : not struct> (app : Falco) =
            Services.add (fun svc -> svc.AddSingleton(serviceType = typeof<'a>, implementationType = typeof<'b>)) app

        static member addTransient<'a, 'b when 'a : not struct and 'b : not struct> (app : Falco) =
            Services.add (fun svc -> svc.AddSingleton(serviceType = typeof<'a>, implementationType = typeof<'b>)) app

        static member addScopedIf<'a, 'b when 'a : not struct and 'b : not struct> (predicate : bool) (app : Falco) =
            Services.addIf predicate (fun svc -> svc.AddScoped(serviceType = typeof<'a>, implementationType = typeof<'b>)) app

        static member addSingletonIf<'a, 'b when 'a : not struct and 'b : not struct> (predicate : bool) (app : Falco) =
            Services.addIf predicate (fun svc -> svc.AddSingleton(serviceType = typeof<'a>, implementationType = typeof<'b>)) app

        static member addTransientIf<'a, 'b when 'a : not struct and 'b : not struct> (predicate : bool) (app : Falco) =
            Services.addIf predicate (fun svc -> svc.AddSingleton(serviceType = typeof<'a>, implementationType = typeof<'b>)) app

        static member addScopedType<'a when 'a : not struct> (app : Falco) =
            Services.add (fun svc -> svc.AddScoped(serviceType = typeof<'a>)) app

        static member addSingletonType<'a when 'a : not struct> (app : Falco) =
            Services.add (fun svc -> svc.AddSingleton(serviceType = typeof<'a>)) app

        static member addTransientType<'a when 'a : not struct> (app : Falco) =
            Services.add (fun svc -> svc.AddSingleton(serviceType = typeof<'a>)) app

        static member addScopedTypeIf<'a when 'a : not struct> (predicate : bool) (app : Falco) =
            Services.addIf predicate (fun svc -> svc.AddScoped(serviceType = typeof<'a>)) app

        static member addSingletonTypeIf<'a when 'a : not struct> (predicate : bool) (app : Falco) =
            Services.addIf predicate (fun svc -> svc.AddSingleton(serviceType = typeof<'a>)) app

        static member addTransientTypeIf<'a when 'a : not struct> (predicate : bool) (app : Falco) =
            Services.addIf predicate (fun svc -> svc.AddSingleton(serviceType = typeof<'a>)) app

        static member addScopedConfigured<'a when 'a : not struct> (fac : IConfiguration -> IServiceProvider -> 'a) (app : Falco) =
            Services.addConfigured (fun conf svc -> svc.AddScoped<'a>(Func<IServiceProvider, 'a>(fac conf))) app

        static member addSingletonConfigured<'a when 'a : not struct> (fac : IConfiguration -> IServiceProvider -> 'a) (app : Falco) =
            Services.addConfigured (fun conf svc -> svc.AddSingleton<'a>(Func<IServiceProvider, 'a>(fac conf))) app

        static member addTransientConfigured<'a when 'a : not struct> (fac : IConfiguration -> IServiceProvider -> 'a) (app : Falco) =
            Services.addConfigured (fun conf svc -> svc.AddTransient<'a>(Func<IServiceProvider, 'a>(fac conf))) app

        static member addScopedConfiguredInf<'a when 'a : not struct> (predicate : bool)  (fac : IConfiguration -> IServiceProvider -> 'a) (app : Falco) =
            Services.addConfiguredIf predicate (fun conf svc -> svc.AddScoped<'a>(Func<IServiceProvider, 'a>(fac conf))) app

        static member addSingletonConfiguredIf<'a when 'a : not struct> (predicate : bool)  (fac : IConfiguration -> IServiceProvider -> 'a) (app : Falco) =
            Services.addConfiguredIf predicate (fun conf svc -> svc.AddSingleton<'a>(Func<IServiceProvider, 'a>(fac conf))) app

        static member addTransientConfiguredIf<'a when 'a : not struct> (predicate : bool)  (fac : IConfiguration -> IServiceProvider -> 'a) (app : Falco) =
            Services.addConfiguredIf predicate (fun conf svc -> svc.AddTransient<'a>(Func<IServiceProvider, 'a>(fac conf))) app

    type Middleware =
        static member add (fn : IApplicationBuilder -> IApplicationBuilder) (app : Falco) =
            app.Config <- { app.Config with Middleware = app.Config.Middleware >> fn }
            app

        static member addIf (predicate : bool) (fn : IApplicationBuilder -> IApplicationBuilder) (app : Falco) =
            if predicate then Middleware.add fn app
            else app

        static member addInline (requestDelegate : RequestDelegate -> RequestDelegate) (app : Falco) =
            let fn (app : IApplicationBuilder) = app.Use(requestDelegate)
            Middleware.add fn app

        static member addInlineIf (predicate : bool) (requestDelegate : RequestDelegate -> RequestDelegate) (app : Falco) =
            if predicate then Middleware.addInline requestDelegate app
            else app

        static member addType<'a> (app : Falco) =
            let fn (app : IApplicationBuilder) = app.UseMiddleware<'a>()
            Middleware.add fn app

        static member addTypeIf<'a> (predicate : bool) (app : Falco) =
            if predicate then Middleware.addType<'a> app
            else app

        static member addProps<'a> ([<ParamArray>] props : obj array) = fun (app : Falco) ->
            let fn (app : IApplicationBuilder) = app.UseMiddleware<'a>(props)
            Middleware.add fn app

        static member addPropsIf<'a> ([<ParamArray>] props : obj array) = fun (predicate : bool) (app : Falco) ->
            if predicate then Middleware.addProps<'a> props app
            else app
