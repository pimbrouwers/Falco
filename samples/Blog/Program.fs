module Blog.Program

open Blog.Server

[<EntryPoint>]
let main args =    
    try
        Host.startHost
            Env.developerMode
            Env.postsDirectory
            args
        0
    with
    | _ -> -1