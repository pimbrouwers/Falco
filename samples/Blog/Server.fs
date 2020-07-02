module Blog.Server

open Falco
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Blog.Post.Middleware
open Blog.Post.Model

module Handlers =
    let handleException (developerMode : bool) : ExceptionHandler =
        fun ex _ -> 
            setStatusCode 500 >=>
            (match developerMode with
            | true  -> textOut (sprintf "Server error: %s\n\n%s" ex.Message ex.StackTrace)
            | false -> textOut "Server Error") 

    let handleNotFound = 
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


let startServer 
    (developerMode : bool) 
    (postsDirectory : string) =            
    try
        // Load all posts from disk once when server starts
        let posts = Post.IO.all postsDirectory

        let routes = 
            [
                get      "/{slug:regex(^[a-z\-])}" (withPost posts Post.Controller.details)
                get      "/"                       (Post.Controller.index posts)
            ]

        WebHostBuilder()
            .UseKestrel()
            .ConfigureServices(Config.configureServices)
            .Configure(Config.configure developerMode routes)
            .Build()
            .Run()
        0
    with           
        | _ -> -1

