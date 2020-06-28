module HelloWorldApp 

open Falco
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

let routes = 
    [
        get "/"    (textOut "hello world")
        get "/exn" (fun _ _ -> failwith "Fake exception")
    ]

let exceptionHandler : ExceptionHandler =
    fun ex _ -> 
        setStatusCode 500 
        >=> textOut (sprintf "Error: %s" ex.Message)

let configureLogging (log : ILoggingBuilder) =
    log.AddConsole()
       .AddDebug()
       .SetMinimumLevel(LogLevel.Warning)
       |> ignore

let configureServices (services : IServiceCollection) =
    services.AddRouting() 
            .AddResponseCompression()
            .AddResponseCaching()
    |> ignore

let configure (app : IApplicationBuilder) = 
    app.UseExceptionMiddleware(exceptionHandler)
       .UseRouting()
       .UseResponseCompression()
       .UseResponseCaching()
       .UseHttpEndPoints(routes)
       .UseNotFoundHandler(setStatusCode 404 >=> textOut "Not found")
       |> ignore 

[<EntryPoint>]
let main _ =
    try
        WebHostBuilder()
            .UseKestrel()
            .ConfigureLogging(configureLogging)
            .ConfigureServices(configureServices)
            .Configure(configure)
            .Build()
            .Run()
        0
    with 
        | _ -> -1