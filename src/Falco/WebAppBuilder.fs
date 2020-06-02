[<AutoOpen>]
module Falco.Builder

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

/// Represents the eventual existence of a runnable IWebhost
type WebApp = 
    {
        HostBuilder   : IWebHostBuilder                
        Configuration : IConfigurationBuilder -> IConfigurationBuilder
        Host          : IWebHostBuilder -> IWebHostBuilder
        Logging       : ILoggingBuilder -> ILoggingBuilder
        Services      : IServiceCollection -> IServiceCollection
        Middleware    : IApplicationBuilder -> IApplicationBuilder
        Routes        : HttpEndpoint list
        ErrorHandler  : ExceptionHandler option
        NotFound      : HttpHandler option
    }

module WebApp =
    let empty () = 
        { 
            Host           = id
            HostBuilder    = WebHostBuilder()            
            Configuration  = id
            Logging        = id
            Services       = id
            Middleware     = id
            Routes         = []
            ErrorHandler   = None
            NotFound       = None            
        }

/// Computation expression to allow for elegant IWebhost construction
type WebAppBuilder() =    
    member __.Yield(_) = WebApp.empty ()

    member __.Run(webApp : WebApp) =                  
        let host = webApp.HostBuilder
        
        host.UseKestrel()     
            
            // Configuration
            .UseConfiguration(
                ConfigurationBuilder() :> IConfigurationBuilder
                |> webApp.Configuration                 
                |> fun config -> config.Build())
            
            // Logging
            .ConfigureLogging(fun logging -> 
                logging
                |> webApp.Logging
                |> ignore)

            // Middleware
            .ConfigureServices(fun services -> 
                services.AddRouting() 
                |> webApp.Services 
                |> ignore)

            // Activate middleware
            .Configure(fun app ->
                // Error Handler
                match webApp.ErrorHandler with
                | Some e -> app.UseMiddleware<ExceptionHandlingMiddleware> e |> ignore
                | None   -> ()
                
                // Activate user middlware
                app
                |> webApp.Middleware
                |> ignore

                // Activate Falco
                app.UseRouting() // we call this just in case, calling multiple times has no side effects
                   .UseHttpEndPoints(webApp.Routes)                   
                   |> fun app -> 
                       // Not Found Handler
                       match webApp.NotFound with
                       | Some nf -> app.UseNotFoundHandler(nf)
                       | None _  -> ())
            
            // User host config
            |> webApp.Host
        |> ignore

        host.Build().Run()
    
    // Host configuration
    [<CustomOperation("host")>]
    member __.Host (app : WebApp, host : IWebHostBuilder -> IWebHostBuilder) =
        { app with Host = app.Host >> host }

    [<CustomOperation("configure")>]
    member __.Configure (app : WebApp, configuration : IConfigurationBuilder -> IConfigurationBuilder) =
        { app with Configuration = app.Configuration >> configuration }

    // Configuration Operations
    [<CustomOperation("logging")>]
    member __.Logging (app : WebApp, logging : ILoggingBuilder -> ILoggingBuilder) =
        { app with Logging = app.Logging >> logging }

    [<CustomOperation("services")>]
    member __.Services (app : WebApp, services : IServiceCollection -> IServiceCollection) =
        { app with Services = app.Services >> services }

    [<CustomOperation("middleware")>]
    member __.Middlware (app: WebApp, middlware : IApplicationBuilder -> IApplicationBuilder) =
        { app with Middleware = app.Middleware >> middlware }
        
    // Error Operations
    [<CustomOperation("errors")>]
    member __.Errors (app : WebApp, handler : ExceptionHandler) =
        { app with ErrorHandler = Some handler }

    [<CustomOperation("notFound")>]
    member __.NotFound (app : WebApp, handler : HttpHandler) =
        { app with NotFound = Some handler }

    // Routing Operations
    [<CustomOperation("falco")>]
    member __.Routes (app : WebApp, routes : HttpEndpoint list) =
        { app with Routes = routes }

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

let webApp = WebAppBuilder()

