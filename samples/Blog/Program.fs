module Blog.Program

open System
open System.IO
open Falco
open Falco.StringUtils
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration

[<EntryPoint>]
let main args =    
    let developerMode : DeveloperMode =         
        tryGetEnv "ASPNETCORE_ENVIRONMENT"
        |> Option.defaultValue "Production"
        |> StringUtils.strEquals "Development"

    let dir = Directory.GetCurrentDirectory()

    let postsDirectory = Path.Combine(dir, "Posts")         

    let contentRoot : ContentRoot = 
        tryGetEnv WebHostDefaults.ContentRootKey 
        |> Option.defaultValue dir

    if developerMode then 
        Console.WriteLine(sprintf "Posts Directory: %s" postsDirectory)

    try
        // Load all posts from disk only once when server starts
        let posts = PostProcessor.loadAll postsDirectory
            
        Host.startWebHost 
            args
            (Server.configure contentRoot developerMode)
            [
                get "/{slug:regex(^[a-z\-])}" 
                    (Post.Controller.details posts)
                
                get "/json" 
                    (Post.Controller.json posts)
                
                get "/" 
                    (Post.Controller.index posts)
            ]
        0
    with
    | _ -> -1