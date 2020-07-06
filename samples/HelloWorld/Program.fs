module HelloWorld.Program

open Falco

let handleHello =
    get "/" (fun ctx ->
        Response.ofPlainText ctx "Hello world")

[<EntryPoint>]
let main args =        
    Host.startDefaultHost 
        args 
        [
            handleHello
        ]
    0