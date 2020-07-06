module HelloWorld.Program

open Falco

let handleHello =
    get "/" (fun ctx ->
        Response.ofPlainText "Hello world" ctx)

[<EntryPoint>]
let main args =        
    Host.startDefaultHost 
        args 
        [
            handleHello
        ]
    0