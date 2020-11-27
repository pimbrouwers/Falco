module AppName.Program

open Falco
open Falco.HostBuilder
open Falco.Routing
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection

// ------------
// Register services
// ------------
let configureServices (services : IServiceCollection) =
    services.AddAntiforgery()
            .AddFalco() |> ignore

// ------------
// Activate middleware
// ------------
let configureApp (endpoints : HttpEndpoint list) (app : IApplicationBuilder) =    
    app.UseStaticFiles()       
       .UseFalco(endpoints) |> ignore

// -----------
// Configure Web host
// -----------
let configureWebHost (endpoints : HttpEndpoint list) (webHost : IWebHostBuilder) =
    webHost
        .ConfigureServices(configureServices)
        .Configure(configureApp endpoints)

[<EntryPoint>]
let main args =    
    webHost args {
        configure configureWebHost
        endpoints [            
            all Urls.``/value/create``
                [
                    GET,  Value.Controller.create
                    POST, Value.Controller.createSubmit
                ]

            get Urls.``/``
                Value.Controller.index
        ]
    }       
    0
    