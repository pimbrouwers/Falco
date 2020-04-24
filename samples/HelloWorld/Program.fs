module HelloWorldApp 

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Falco

// ------------
// Handlers & Routes
// ------------
let helloHandler : HttpHandler =
    textOut "hello world"

let routes = [        
    get "/" helloHandler
]

// ------------
// Web App
// ------------
let configureApp (app : IApplicationBuilder) =     
    app.UseDeveloperExceptionPage()     
       .UseRouting()
       .UseHttpEndPoints(routes)
       .UseNotFoundHandler(setStatusCode 404 >=> textOut "Not found")
       |> ignore

// ------------
// Logging
// ------------
let configureLogging (loggerBuilder : ILoggingBuilder) =
    loggerBuilder
        .AddFilter(fun l -> l.Equals LogLevel.Information)
        .AddConsole()
        .AddDebug() |> ignore

// ------------
// Services
// ------------
let configureServices (services : IServiceCollection) =
    services        
        .AddRouting()        
        |> ignore


[<EntryPoint>]
let main _ =
    try
        WebHostBuilder()
            .UseKestrel()       
            .ConfigureLogging(configureLogging)
            .ConfigureServices(configureServices)
            .Configure(configureApp)          
            .Build()
            .Run()
        0
    with 
        | _ -> -1