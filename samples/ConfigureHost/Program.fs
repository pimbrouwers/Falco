module ConfigureHost.Program

open Falco
open Falco.Markup
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

// ------------
// Handlers 
// ------------
let handlePlainText : HttpHandler =
    "Hello from /"
    |> Response.ofPlainText 

let handleHtml : HttpHandler =
    Templates.html5 "en" [] [ Elem.h1 [] [ Text.raw "Hello from /html" ] ]
    |> Response.ofHtml

let handleJson : HttpHandler =
    {| Message = "Hello from /json" |}
    |> Response.ofJson 

let handleGreeting : HttpHandler =
    Request.mapRoute 
        (fun r -> r.Get "name" "John Doe" |> sprintf "Hi %s") 
        Response.ofPlainText

// ------------
// Logging 
// ------------
let configureLogging (log : ILoggingBuilder) =
    log.ClearProviders()
       .AddConsole()
       .SetMinimumLevel(LogLevel.Error)
    |> ignore

// ------------
// Register services
// ------------
let configureServices (services : IServiceCollection) =
    services.AddFalco() |> ignore

// ------------
// Activate middleware
// ------------
let configureApp (endpoints : HttpEndpoint list) (ctx : WebHostBuilderContext) (app : IApplicationBuilder) =    
    let devMode = StringUtils.strEquals ctx.HostingEnvironment.EnvironmentName "Development"    
    app.UseWhen(devMode, fun app -> 
            app.UseDeveloperExceptionPage())
       .UseWhen(not(devMode), fun app -> 
            app.UseFalcoExceptionHandler(Response.withStatusCode 500 >> Response.ofPlainText "Server error"))
       .UseFalco(endpoints) |> ignore

// -----------
// Configure Host
// -----------
let configureHost (endpoints : HttpEndpoint list) (webhost : IWebHostBuilder) =
    webhost.ConfigureLogging(configureLogging)
           .ConfigureServices(configureServices)
           .Configure(configureApp endpoints)

[<EntryPoint>]
let main args =    
    webHost args {
        configure configureHost
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