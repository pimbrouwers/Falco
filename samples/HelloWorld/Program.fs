module HelloWorldApp 

open Falco
open Microsoft.AspNetCore.Builder
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
    |> ignore

let configure (app : IApplicationBuilder) = 
    app.UseExceptionMiddleware(exceptionHandler)
       .UseRouting()
       .UseHttpEndPoints(routes)
       .UseNotFoundHandler(setStatusCode 404 >=> textOut "Not found")
       |> ignore 

[<EntryPoint>]
let main _ =
    try
        startWebApp
            configure
            configureServices
            (Some configureLogging)
            None
        0
    with 
        | _ -> -1