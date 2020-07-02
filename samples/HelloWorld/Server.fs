module HelloWorld.Server

open Falco
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection

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
    
let startServer (developerMode : bool) =            
    let routes = 
        [
            get "/"    (textOut "hello world")                
        ]

    try
        WebHostBuilder()
            .UseKestrel()
            .ConfigureServices(Config.configureServices)
            .Configure(Config.configure developerMode routes)
            .Build()
            .Run()
        0
    with 
        | _ -> -1
