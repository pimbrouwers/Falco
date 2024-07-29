open Falco
open Microsoft.AspNetCore.Builder // <-- this import adds many useful extensions

let wapp = WebApplication.Create()

let endpoints = 
    [ 
        get "/" (Response.ofPlainText "Hello World!") // <-- associate GET / to plain text HttpHandler
    ]

wapp.UseFalco(endpoints) // <-- activate Falco endpoint source
    .Run()
