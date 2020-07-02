module HelloWorld.Server

open Falco
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

/// DeveloperMode is a wrapped boolean primitive to
/// define the status of "developer mode".
[<Struct>]
type DeveloperMode = DeveloperMode of bool

/// BuildServer defines a function with a dependency
/// which returns an IWebHost instance.
type BuildServer = DeveloperMode -> unit

module Handlers = 
    let handleException (developerMode : bool) : ExceptionHandler =
        fun ex _ -> 
            setStatusCode 500 >=>
            (match developerMode with
            | true  -> textOut (sprintf "Server error: %s\n\n%s" ex.Message ex.StackTrace)
            | false -> textOut "Server Error") 
        
    let handleNotFound : HttpHandler =
        setStatusCode 404
        >=> textOut "Not found"

module Config = 
    let configureServices (services : IServiceCollection) =
        services.AddRouting() 
        |> ignore

    let configure 
        (developerMode : bool)
        (routes : HttpEndpoint list)
        (app : IApplicationBuilder) = 
        app.UseExceptionMiddleware(Handlers.handleException developerMode)
            .UseRouting()
            .UseHttpEndPoints(routes)
            .UseNotFoundHandler(Handlers.handleNotFound)
            |> ignore 
    
let buildServer (webHost : IWebHostBuilder) : BuildServer =            
    fun (developerMode : DeveloperMode) ->
        // unwrap our constrained type
        let (DeveloperMode developerMode) = developerMode

        let routes = 
            [
                get "/"    (textOut "hello world")                
            ]

        webHost
            .UseKestrel()            
            .ConfigureServices(Config.configureServices)
            .Configure(Config.configure developerMode routes)
            |> ignore
    