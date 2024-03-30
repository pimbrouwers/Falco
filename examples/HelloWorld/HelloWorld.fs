open Falco
open Microsoft.AspNetCore.Builder // <-- this import adds many useful extensions

let wapp = WebApplication.Create()

wapp.UseFalco() // <-- activate Falco endpoint source
    .FalcoGet("/", Response.ofPlainText "hello world") // <-- associate GET / to plain text HttpHandler
    .Run()
