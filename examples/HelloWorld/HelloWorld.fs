open Falco
open Falco.Routing
open Microsoft.AspNetCore.Builder // <-- this import adds many useful extensions

let bldr = WebApplication.CreateBuilder()
let wapp = bldr.Build()

let endpoints =
    // associate GET / to plain text HttpHandler
    [ get "/" (Response.ofPlainText "Hello World!") ]

// activate Falco endpoint source
wapp.UseFalco(endpoints)
    .Run()
