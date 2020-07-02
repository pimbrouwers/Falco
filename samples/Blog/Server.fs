module Blog.Server

open Falco
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Blog.Post.Middleware

/// DeveloperMode is a wrapped boolean primitive to
/// isolate the status of "developer mode".
[<Struct>]
type DeveloperMode = DeveloperMode of bool

/// PostsDirectory is a wrapped string literal to
/// isolate the posts directory.
[<Struct>]
type PostsDirectory = PostsDirectory of string

/// BuildServer defines a function with dependencies
/// which returns an IWebHost instance.
type BuildServer = DeveloperMode -> PostsDirectory -> IWebHost

/// StartServer defines a function which starts the provided
/// instance of IWebHost.
type StartServer = IWebHost -> unit

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

let buildServer : BuildServer =            
    fun (developerMode : DeveloperMode) (postsDirectory : PostsDirectory) ->
        // unwrap our constrained types
        let (DeveloperMode developerMode) = developerMode
        let (PostsDirectory postsDirectory) = postsDirectory
        
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

let startServer : StartServer =
    fun (server : IWebHost) ->
        server.Run()
