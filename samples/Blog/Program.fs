module Blog.Program

open Falco

[<EntryPoint>]
let main args =    
    try
        // Load all posts from disk only once when server starts
        let posts = Post.Data.loadAll Env.postsDirectory
            
        Host.startWebHost 
            args
            (Server.configureWebHost Env.developerMode)
            [
                get "/{slug:regex(^[a-z\-])}" (Post.Controller.details posts)
                get "/json" (Post.Controller.json posts)
                get "/" (Post.Controller.index posts)
            ]
        0
    with
    | _ -> -1