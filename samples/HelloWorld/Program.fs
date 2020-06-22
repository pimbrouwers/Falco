module HelloWorldApp 

open Falco
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection

let routes = 
    [
        get "/"  (textOut "hello world")
    ]

let configureServices (services : IServiceCollection) =
    services.AddRouting() 
    |> ignore

let configureApp (app : IApplicationBuilder) = 
    app.UseRouting()
       .UseHttpEndPoints(routes)
       .UseNotFoundHandler(setStatusCode 404 >=> textOut "Not found")
       |> ignore 

[<EntryPoint>]
let main _ =
    try
        WebHostBuilder()
            .UseKestrel()
            .ConfigureServices(configureServices)
            .Configure(configureApp)
            .Build()
            .Run()

        0
    with 
        | _ -> -1