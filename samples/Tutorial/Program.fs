module FalcoTutorial.Program

open Falco
open Falco.Markup
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder

let exceptionHandler : HttpHandler =
    Response.withStatusCode 500 >> Response.ofPlainText "Server error"

[<EntryPoint>]
let main args =
    webHost args {
        use_ifnot FalcoExtensions.IsDevelopment HstsBuilderExtensions.UseHsts
        use_https
        use_static_files

        endpoints []
    }
    0