module HelloWorld.Program

module Env =    
    open System    
    open Falco.StringUtils

    let tryGetEnv = Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x

    let developerMode = match tryGetEnv "ASPNETCORE_ENVIRONMENT" with None -> true | Some env -> strEquals env "development"

[<EntryPoint>]
let main _ =        
    Server.startServer 
        Env.developerMode       