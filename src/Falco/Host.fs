module Falco.Host

open System    
open Falco    
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting    
open Microsoft.Extensions.DependencyInjection    
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

type ConfigureWebHost = HttpEndpoint list -> IWebHostBuilder -> unit

let defaultExceptionHandler 
    (ex : Exception)
    (log : ILogger) : HttpHandler =
    let logMessage = sprintf "Server error: %s\n\n%s" ex.Message ex.StackTrace
    log.Log(LogLevel.Error, logMessage)        
    
    Response.withStatusCode 500
    >> Response.ofPlainText logMessage
        
let defaultNotFoundHandler : HttpHandler =    
    Response.withStatusCode 404
    >> Response.ofPlainText "Not found"

let startWebHost =
    fun (args : string[]) 
        (webHostBuilder : ConfigureWebHost)
        (endpoints : HttpEndpoint list) ->    
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHost(Action<IWebHostBuilder> (webHostBuilder endpoints))
        .Build()
        .Run()

let defaultConfigureWebHost = 
    let configureLogging
            (log : ILoggingBuilder) =
            log.SetMinimumLevel(LogLevel.Error)
            |> ignore

    let configureServices 
        (services : IServiceCollection) =
        services.AddRouting()     
                .AddResponseCaching()
                .AddResponseCompression()
        |> ignore
                    
    let configure             
        (routes : HttpEndpoint list)
        (app : IApplicationBuilder) =         
        app.UseExceptionMiddleware(defaultExceptionHandler)
            .UseResponseCaching()
            .UseResponseCompression()
            .UseStaticFiles()
            .UseRouting()
            .UseHttpEndPoints(routes)
            .UseNotFoundHandler(defaultNotFoundHandler)
            |> ignore 
                     
    fun (endpoints : HttpEndpoint list)
        (webHost : IWebHostBuilder) ->  
        webHost
            .UseKestrel()
            .ConfigureLogging(configureLogging)
            .ConfigureServices(configureServices)
            .Configure(configure endpoints)
            |> ignore

let startWebHostDefault =
    fun (args : string[]) 
        (endpoints : HttpEndpoint list) ->
            
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHost(Action<IWebHostBuilder> (defaultConfigureWebHost endpoints))
        .Build()
        .Run()
