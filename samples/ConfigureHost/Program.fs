module ConfigureHost.Program

open Falco
open Falco.Markup
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder

// ------------
// Handlers 
// ------------
let handlePlainText : HttpHandler =
    "Hello from /"
    |> Response.ofPlainText 

let handleHtml : HttpHandler =
    Templates.html5 "en" 
        [ Elem.link [ Attr.href "style.css"; Attr.rel "stylesheet" ] ]
        [ Elem.h1 [] [ Text.raw "Hello from /html" ] ]
    |> Response.ofHtml

let handleJson : HttpHandler =
    {| Message = "Hello from /json" |}
    |> Response.ofJson 

let handleGreeting : HttpHandler =
    Request.mapRoute 
        (fun r -> r.Get "name" "John Doe" |> sprintf "Hi %s") 
        Response.ofPlainText

let exceptionHandler : HttpHandler =
    Response.withStatusCode 500 >> Response.ofPlainText "Server error"

// ------------
// Host
// ------------
[<EntryPoint>]
let main args =     
    webHost args {
        use_ifnot FalcoExtensions.IsDevelopment HstsBuilderExtensions.UseHsts
        use_https
        use_compression
        use_static_files

        use_if    FalcoExtensions.IsDevelopment DeveloperExceptionPageExtensions.UseDeveloperExceptionPage
        use_ifnot FalcoExtensions.IsDevelopment (FalcoExtensions.UseFalcoExceptionHandler exceptionHandler)

        endpoints [            
            get "/greet/{name:alpha}" 
                handleGreeting

            get "/json" 
                handleJson

            get "/html" 
                handleHtml
                
            get "/" 
                handlePlainText
        ]
    }
    0