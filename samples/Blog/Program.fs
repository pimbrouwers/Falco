module Blog.Program

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Falco
open Blog.Handlers
open Microsoft.AspNetCore.Hosting

let routes = [
    get      "/{slug:regex(^[a-z\-])}" blogPostHandler
    get      "/"                       blogIndexHandler
]

let configureLogging (log : ILoggingBuilder) =
    log.AddConsole()
       .AddDebug()
       |> ignore

let configureServices (svc : IServiceCollection) =
    svc.AddResponseCompression()
       .AddResponseCaching()
       .AddRouting()            
    |> ignore

let configureApp (app : IApplicationBuilder) = 
    let notFoundHandler : HttpHandler =
        setStatusCode 404 
        >=> textOut "NotFound"

    let exceptionHandler : ExceptionHandler =
        fun ex _ -> 
            setStatusCode 500 
            >=> textOut (sprintf "Error: %s" ex.Message)

    app.UseRouting()
       .UseExceptionMiddleware(exceptionHandler)
       .UseHttpEndPoints(routes)
       .UseNotFoundHandler(notFoundHandler)
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