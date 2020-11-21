namespace Falco.Custom

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

open Falco
open Falco.Host
open Falco.Markup

module Main = 
    // Logging
    let configureLogging 
        (log : ILoggingBuilder) =
        log.SetMinimumLevel(LogLevel.Error)
        |> ignore

    // Services
    let configureServices 
        (services : IServiceCollection) =
        services.AddRouting()     
                .AddResponseCaching()
                .AddResponseCompression()
        |> ignore

    // Middleware
    let configure                 
        (endpoints : HttpEndpoint list)
        (app : IApplicationBuilder) = 
                
        app.UseExceptionMiddleware(Host.defaultExceptionHandler)
            .UseResponseCaching()
            .UseResponseCompression()
            .UseStaticFiles()
            .UseRouting()
            .UseHttpEndPoints(endpoints)
            .UseNotFoundHandler(Host.defaultNotFoundHandler)
            |> ignore 

    // Web Host
    let configureWebHost : ConfigureWebHost =
        fun (endpoints : HttpEndpoint list) 
            (host : IWebHostBuilder) ->
            host
                .UseKestrel()
                .ConfigureLogging(configureLogging)
                .ConfigureServices(configureServices)
                .Configure(configure endpoints)
                |> ignore

    [<EntryPoint>]
    let main args =    
        Host.startWebHost 
            args
            configureWebHost
            [
                get "/json" 
                    (Response.ofJson {| Message = "Hello from /json" |})

                get "/html" 
                    (Response.ofHtml (Templates.html5 "en" [] [ Elem.h1 [] [ Text.raw "Hello from /html" ] ]))

                get "/" 
                    (Response.ofPlainText "Hello from /")
            ]
        0