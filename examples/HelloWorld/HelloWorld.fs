open Falco
open Microsoft.AspNetCore.Builder // <-- this import adds many useful extensions
open Microsoft.Extensions.Configuration

let bldr = WebApplication.CreateBuilder()
let conf =
    bldr.Configuration
        .AddJsonFile("appsettings.json", optional = false)
        .AddJsonFile("appsettings.Development.json")

let wapp = WebApplication.Create()

let endpoints =
    // associate GET / to plain text HttpHandler
    [ get "/" (Response.ofPlainText "Hello World!") ]

// activate Falco endpoint source
wapp.UseFalco(endpoints)
    .Run()
