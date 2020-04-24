module Blog.Program

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Falco
open Blog.Handlers

// ------------
// Web App
// ------------
let configureApp (app : IApplicationBuilder) =     
    let routes = [        
        get "/{slug:regex(^[a-z\-])}" blogPostHandler
        any "/"                       blogIndexHandler
    ]
    
    app.UseDeveloperExceptionPage()       
       .UseRouting()
       .UseHttpEndPoints(routes)
       .UseNotFoundHandler(notFoundHandler)
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