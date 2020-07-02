module Blog.Program

open Blog.Server

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
    try
        let developerMode = DeveloperMode Env.developerMode
        let postsDirectory = PostsDirectory Env.postsDirectory

        Server.buildServer developerMode postsDirectory
        |> Server.startServer
        0
    with
    | _ -> -1