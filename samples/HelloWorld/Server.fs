module HelloWorld.Server

module Router = 
    open Falco 

    let endpoints = 
        [
            get      "/" (textOut "Hello, world!")                    
        ]

module Handlers =   
    open Falco

    let handleException (developerMode : bool) : ExceptionHandler =
        fun ex _ -> 
            setStatusCode 500 >=>
            (match developerMode with
            | true  -> textOut (sprintf "Server error: %s\n\n%s" ex.Message ex.StackTrace)
            | false -> textOut "Server Error") 

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
            (developerMode : bool)
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
            let (DeveloperMode developerMode) = developerMode        
            
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
