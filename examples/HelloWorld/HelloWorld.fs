open Falco
open Falco.Routing
open Microsoft.AspNetCore.Builder

let wapp = WebApplication.Create()

wapp.UseRouting()
    .UseFalco([
        get "/" (Response.ofPlainText "Hello World!")
    ])
    .Run(Response.ofPlainText "Not found")
