module HelloWorld.Program

open HelloWorld.Server

[<EntryPoint>]
let main args =        
    try
        Host.startHost
            Env.developerMode            
            args
        0
    with
    | _ -> -1