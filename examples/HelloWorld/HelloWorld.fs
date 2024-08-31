open Falco
open Microsoft.AspNetCore.Builder // <-- this import adds many useful extensions

let wapp = WebApplication.Create()

let endpoints =
    // associate GET / to plain text HttpHandler
    [ get "/" (Response.ofPlainText "Hello World!") ]

// activate Falco endpoint source
wapp.UseFalco(endpoints)
    .Run()
