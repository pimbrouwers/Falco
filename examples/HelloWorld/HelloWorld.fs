open Falco
open Falco.Routing
open Microsoft.AspNetCore.Builder
// ^-- this import adds many useful extensions

let endpoints =
    [
        get "/" (Response.ofPlainText "Hello World!")
        // ^-- associate GET / to plain text HttpHandler
    ]

let wapp = WebApplication.Create()

wapp.UseRouting()
    .UseFalco(endpoints)
    // ^-- activate Falco endpoint source
    .Run()
