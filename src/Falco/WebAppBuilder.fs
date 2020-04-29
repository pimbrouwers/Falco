[<AutoOpen>]
module Falco.Builder

open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

/// Represents the eventual existence of a runnable IWebhost
type WebApp = 
    {
        Configurations : IApplicationBuilder -> IApplicationBuilder
        ContentRoot    : string
        ErrorHandler   : ExceptionHandler option
        HostBuilder    : IWebHostBuilder
        Logging        : ILoggingBuilder -> ILoggingBuilder
        NotFound       : HttpHandler option
        Routes         : HttpEndpoint list
        Services       : IServiceCollection -> IServiceCollection
        WebRoot        : string option
    }

    static member Empty () = 
        { 
            Configurations = id
            ContentRoot    = Directory.GetCurrentDirectory()
            ErrorHandler   = None
            HostBuilder    = WebHostBuilder().UseKestrel()
            Logging        = id
            NotFound       = None
            Routes         = []
            Services       = id
            WebRoot        = None
        }

/// Computation expression to allow for elegant IWebhost construction
type WebAppBuilder() =    
    member __.Yield(_) = WebApp.Empty ()

    member __.Run(webApp : WebApp) =          
        let defaultWebRootDir = "wwwroot"
        let host = webApp.HostBuilder
        
        host.UseContentRoot(webApp.ContentRoot)
            .UseWebRoot(webApp.WebRoot |> Option.defaultValue (Path.Join(webApp.ContentRoot, defaultWebRootDir)))
            .ConfigureLogging(fun logging -> 
                // Logging
                logging
                |> webApp.Logging
                |> ignore)

            .ConfigureServices(fun services -> 
                // Middleware
                services.AddRouting() 
                |> webApp.Services 
                |> ignore)

            .Configure(fun app ->
                // Error Handler
                match webApp.ErrorHandler with
                | Some e -> app.UseMiddleware<ExceptionHandlingMiddleware> e |> ignore
                | None   -> ()
                
                // Routes & Activation
                app.UseRouting()
                   .UseHttpEndPoints(webApp.Routes)
                |> webApp.Configurations
                |> fun _ -> 
                    // Not Found Handler
                    match webApp.NotFound with
                    | Some nf -> app.UseNotFoundHandler(nf)
                    | None _  -> ())
        |> ignore

        host.Build().Run()
    
    // Configuration Operations
    [<CustomOperation("configure")>]
    member __.Configure (app : WebApp, appConfig : IApplicationBuilder -> IApplicationBuilder) =
        { app with Configurations = app.Configurations >> appConfig }
        
    [<CustomOperation("logging")>]
    member __.Logging (app : WebApp, logConfig : ILoggingBuilder -> ILoggingBuilder) =
        { app with Logging = app.Logging >> logConfig }

    [<CustomOperation("services")>]
    member __.Services (app : WebApp, servicesConfig : IServiceCollection -> IServiceCollection) =
        { app with Services = app.Services >> servicesConfig }

    // Error Operations
    [<CustomOperation("errors")>]
    member __.Errors (app : WebApp, handler : ExceptionHandler) =
        { app with ErrorHandler = Some handler }

    [<CustomOperation("notFound")>]
    member __.NotFound (app : WebApp, handler : HttpHandler) =
        { app with NotFound = Some handler }

    // Routing Operations
    [<CustomOperation("route")>]
    member __.Route (app : WebApp, verb : HttpVerb, pattern : string, handler : HttpHandler) =
        { app with Routes = route verb pattern handler :: app.Routes }

    [<CustomOperation("any")>]
    member __.Any (app : WebApp, pattern : string, handler : HttpHandler) =        
        { app with Routes = any pattern handler :: app.Routes }
    
    [<CustomOperation("get")>]
    member __.Get (app : WebApp, pattern : string, handler : HttpHandler) =
        { app with Routes = get pattern handler :: app.Routes }
    
    [<CustomOperation("head")>]
    member __.Head (app : WebApp, pattern : string, handler : HttpHandler) =
        { app with Routes = head pattern handler :: app.Routes }
    
    [<CustomOperation("post")>]
    member __.Post (app : WebApp, pattern : string, handler : HttpHandler) =
        { app with Routes = post pattern handler :: app.Routes }
    
    [<CustomOperation("put")>]
    member __.Put (app : WebApp, pattern : string, handler : HttpHandler) =
        { app with Routes = put pattern handler :: app.Routes }
    
    [<CustomOperation("patch")>]
    member __.Patch (app : WebApp, pattern : string, handler : HttpHandler) =
        { app with Routes = patch pattern handler :: app.Routes }
    
    [<CustomOperation("delete")>]
    member __.Delete (app : WebApp, pattern : string, handler : HttpHandler) =
        { app with Routes = delete pattern handler :: app.Routes }
    
    [<CustomOperation("options")>]
    member __.Options (app : WebApp, pattern : string, handler : HttpHandler) =
        { app with Routes = options pattern handler :: app.Routes }
    
    [<CustomOperation("trace")>]
    member __.Trace (app : WebApp, pattern : string, handler : HttpHandler) =
        { app with Routes = trace pattern handler :: app.Routes }

    // Directory Configuration   
    [<CustomOperation("contentRoot")>]
    member __.ContentRoot (app : WebApp, dir : string) =
        { app with ContentRoot = dir }

    [<CustomOperation("webRoot")>]
    member __.WebRoot (app : WebApp, dir : string) =
        { app with WebRoot = Some dir }

let webApp = WebAppBuilder()

