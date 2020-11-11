[<AutoOpen>]
module Blog.Common

open System

type DeveloperMode = bool
type ContentRoot = string
type PostsDirectory = string

let tryGetEnv (name : string) = 
    match Environment.GetEnvironmentVariable name with 
    | null 
    | ""    -> None 
    | value -> Some value


