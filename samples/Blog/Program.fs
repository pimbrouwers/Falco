module Blog.Program

open System.IO
open Falco
open Falco.Routing
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Blog.Domain

// ------------
// Routes
// ------------
let endpoints posts = 
    [
        get "/{slug:regex(^[a-z\-])}" 
            (Post.Controller.details posts)
    
        all "/json"
            [ 
                handle POST (Post.Controller.json posts)
                handle GET  Response.ofEmpty
            ]

        get "/json" 
            (Post.Controller.json posts)
    
        get "/" 
            (Post.Controller.index posts)
    ]

// ------------
// Register services
// ------------
let configureServices (services : IServiceCollection) =
    services.AddResponseCaching()
            .AddResponseCompression()
            .AddFalco() 
            |> ignore

// ------------
// Activate middleware
// ------------
let configureApp (posts : Post list) =    
    fun (ctx : WebHostBuilderContext) (app : IApplicationBuilder) ->
        let devMode = StringUtils.strEquals ctx.HostingEnvironment.EnvironmentName "Development"    
        app.UseWhen(devMode, fun app -> 
                app.UseDeveloperExceptionPage())
           .UseWhen(not(devMode), fun app -> 
                app.UseFalcoExceptionHandler(Response.withStatusCode 500 >> Response.ofPlainText "Server error"))
           .UseStaticFiles()
           .UseFalco(endpoints posts) 
           |> ignore

[<EntryPoint>]
let main args =        
    let postsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Posts")         

    try
        // Load all posts from disk only once when server starts
        let posts = PostProcessor.loadAll postsDirectory
               
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(fun webhost ->   
                webhost
                    .ConfigureServices(configureServices)
                    .Configure(configureApp posts)
                    |> ignore)
            .Build()
            .Run()
        0
    with
    | ex -> 
        printfn "%s\n\n%s" ex.Message ex.StackTrace
        -1