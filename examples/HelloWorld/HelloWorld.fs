open Falco
open Microsoft.AspNetCore.Builder // <-- this import adds many useful extensions

let bldr = WebApplication.CreateBuilder()
let wapp = bldr.Build()

let endpoints =
    // associate GET / to plain text HttpHandler
    [ Routing.get "/" (Response.ofPlainText "Hello World!") ]

// activate Falco endpoint source
wapp.UseFalco(endpoints)
    .Run()
