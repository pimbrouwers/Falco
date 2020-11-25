module Blog.Program

open System.IO
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection

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
let configureApp (endpoints : HttpEndpoint list) =    
    fun (ctx : WebHostBuilderContext) (app : IApplicationBuilder) ->
        let devMode = StringUtils.strEquals ctx.HostingEnvironment.EnvironmentName "Development"    
        app.UseWhen(devMode, fun app -> 
                app.UseDeveloperExceptionPage())
           .UseWhen(not(devMode), fun app -> 
                app.UseFalcoExceptionHandler(Response.withStatusCode 500 >> Response.ofPlainText "Server error"))
           .UseStaticFiles()
           .UseFalco(endpoints) 
           |> ignore

// ------------
// Configure web host
// ------------
let configureWebHost (endpoints : HttpEndpoint list) (webhost : IWebHostBuilder) =
    webhost.ConfigureServices(configureServices)
           .Configure(configureApp endpoints)

[<EntryPoint>]
let main args =        
    let postsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Posts")         

    // Load all posts from disk only once when server starts
    let posts = PostProcessor.loadAll postsDirectory
               
    webHost args {
        configure configureWebHost

        endpoints [
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
    }
    0