module HelloWorld.Server

module Router = 
    open Falco 

    let endpoints = 
        [
            get "/" (textOut "Hello, world!")                    
        ]

module Handlers =   
    open Falco

    let handleException (developerMode : DeveloperMode) : ExceptionHandler =
        let (DeveloperMode developerMode) = developerMode

        fun ex _ -> 
            setStatusCode 500 
            >=> textOut (match developerMode with
                        | true  -> sprintf "Server error: %s\n\n%s" ex.Message ex.StackTrace
                        | false -> "Server Error")
            
    let handleNotFound = 
        setStatusCode 404 
        >=> textOut "Not found"

module Host =
    open System
    open Falco    
    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.Hosting    
    open Microsoft.Extensions.DependencyInjection    
    open Microsoft.Extensions.Hosting
    
    type BuildHost = DeveloperMode -> IWebHostBuilder -> unit

    type StartHost = DeveloperMode -> string[] -> unit

    module Config =
        let configureServices (services : IServiceCollection) =
            services.AddRouting()                 
            |> ignore
                        
        let configure             
            (developerMode : DeveloperMode)
            (routes : HttpEndpoint list)
            (app : IApplicationBuilder) =             
            app.UseExceptionMiddleware(Handlers.handleException developerMode)               
               .UseRouting()
               .UseHttpEndPoints(routes)
               .UseNotFoundHandler(Handlers.handleNotFound)
               |> ignore 
    
    let buildHost : BuildHost =            
        fun (developerMode : DeveloperMode)             
            (webHost : IWebHostBuilder) ->        
            webHost
                .UseKestrel()
                .ConfigureServices(Config.configureServices)
                .Configure(Config.configure developerMode Router.endpoints)
                |> ignore

    let startHost : StartHost =
        fun (developerMode : DeveloperMode)             
            (args : string[]) ->
            let configureWebHost (webHost : IWebHostBuilder) =
                buildHost developerMode webHost
            
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHost(Action<IWebHostBuilder> configureWebHost)
                .Build()
                .Run()
