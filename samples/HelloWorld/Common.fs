[<AutoOpen>]
module HelloWorld.Common

[<Struct>]
type DeveloperMode = DeveloperMode of bool

module Env =
    open System    
    open Falco.StringUtils

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


