module Blog.Server

module Router =
    open Falco 
    
    let endpoints posts = 
        [
            get      "/{slug:regex(^[a-z\-])}" (posts |> Post.Controller.details)
            get      "/"                       (posts |> Post.Controller.index)
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
    
    type BuildHost = DeveloperMode -> PostsDirectory -> IWebHostBuilder -> unit

    type StartHost = DeveloperMode -> PostsDirectory -> string[] -> unit

    module Config =
        let configureServices (services : IServiceCollection) =
            services.AddRouting()     
                    .AddResponseCompression()
                    .AddResponseCaching()
            |> ignore
                        
        let configure             
            (developerMode : bool)
            (routes : HttpEndpoint list)
            (app : IApplicationBuilder) = 
            
            app.UseExceptionMiddleware(Handlers.handleException developerMode)
               .UseResponseCompression()
               .UseResponseCaching()
               .UseStaticFiles()
               .UseRouting()
               .UseHttpEndPoints(routes)
               .UseNotFoundHandler(Handlers.handleNotFound)
               |> ignore 
    
    let buildHost : BuildHost =            
        fun (developerMode : DeveloperMode) 
            (postsDirectory : PostsDirectory) 
            (webHost : IWebHostBuilder) ->        
            let (DeveloperMode developerMode) = developerMode        
            let (PostsDirectory postsDirectory) = postsDirectory

            // Load all posts from disk once when server starts
            let posts = Post.Data.loadAll postsDirectory
        
            webHost
                .UseKestrel()
                .ConfigureServices(Config.configureServices)
                .Configure(Config.configure developerMode (Router.endpoints posts))
                |> ignore

    let startHost : StartHost =
        fun (developerMode : DeveloperMode) 
            (postsDirectory : PostsDirectory) 
            (args : string[]) ->
            let configureWebHost (webHost : IWebHostBuilder) =
                buildHost developerMode postsDirectory webHost
            
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHost(Action<IWebHostBuilder> configureWebHost)
                .Build()
                .Run()
