[<AutoOpen>]
module Blog.Common

[<Struct>]
type DeveloperMode = DeveloperMode of bool

[<Struct>]
type PostsDirectory = PostsDirectory of string

module Env =
    open System
    open System.IO
    open Falco.StringUtils
    
    let root = Directory.GetCurrentDirectory()

    let postsDirectory = 
        Path.Combine(root, "Posts") 
        |> PostsDirectory
    
    let tryGetEnv (name : string) = 
        match Environment.GetEnvironmentVariable name with 
        | null 
        | ""    -> None 
        | value -> Some value

    let developerMode = 
        match tryGetEnv "ASPNETCORE_ENVIRONMENT" with 
        | None     -> true 
        | Some env -> strEquals env "development" 
        |> DeveloperMode


