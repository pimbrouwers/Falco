module Blog.Program

module Env =
    open System
    open System.IO
    open Falco.StringUtils

    let root = Directory.GetCurrentDirectory()

    let postsDirectory = Path.Combine(root, "Posts")
    
    let tryGetEnv = Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x

    let developerMode = match tryGetEnv "ASPNETCORE_ENVIRONMENT" with None -> true | Some env -> strEquals env "development"

[<EntryPoint>]
let main _ =
    Server.startServer 
        Env.developerMode
        Env.postsDirectory