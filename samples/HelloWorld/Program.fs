module HelloWorld.Program

open Microsoft.Extensions.Hosting

module Env =    
    open System    
    open Falco.StringUtils
    open HelloWorld.Server
    
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

[<EntryPoint>]
let main args =        
    try
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHost(fun webHost -> Server.buildServer webHost Env.developerMode)
            .Build()
            .Run()
        0
    with
    | _ -> -1