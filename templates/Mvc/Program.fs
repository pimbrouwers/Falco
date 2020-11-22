module AppName.Program

open Falco
open Falco.Routing
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

// ------------
// Web app
// ------------
let endpoints =
    [            
        all Urls.``/value/create``
            [
                handle GET  Value.Controller.create
                handle POST Value.Controller.createSubmit
            ]
        get Urls.``/``
            Value.Controller.index
    ]

// ------------
// Register services
// ------------
let configureServices (services : IServiceCollection) =
    services.AddAntiforgery()
            .AddFalco() |> ignore

// ------------
// Activate middleware
// ------------
let configureApp (app : IApplicationBuilder) =    
    app.UseStaticFiles()       
       .UseFalco(endpoints) |> ignore

[<EntryPoint>]
let main args =    
    try
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(fun webhost ->   
                webhost
                    .ConfigureServices(configureServices)
                    .Configure(configureApp)
                    |> ignore)
            .Build()
            .Run()                        
        0
    with 
    | ex -> 
        printfn "%s\n\n%s" ex.Message ex.StackTrace
        -1