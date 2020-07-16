module Falco.Host

open System    
open Falco    
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting    
open Microsoft.Extensions.DependencyInjection    
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

/// Specifies the process of configuring the IWebHost builder
type ConfigureWebHost = HttpEndpoint list -> IWebHostBuilder -> unit

/// The default exception handler, attempts to logs exception (if exists) and returns HTTP 500
let defaultExceptionHandler 
    (ex : Exception)
    (log : ILogger) : HttpHandler =
    let logMessage = sprintf "Server error: %s\n\n%s" ex.Message ex.StackTrace
    log.Log(LogLevel.Error, logMessage)        
    
    Response.withStatusCode 500
    >> (fun ctx -> ctx.Response.CompleteAsync ())
        
/// Returns HTTP 404
let defaultNotFoundHandler : HttpHandler =    
    Response.withStatusCode 404
    >> (fun ctx -> ctx.Response.CompleteAsync ())

/// Create and start a new IHost (Alias for Host.CreateDefaultBuilder(args))
let startWebHost =
    fun (args : string[]) 
        (webHostBuilder : ConfigureWebHost)
        (endpoints : HttpEndpoint list) ->    
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHost(Action<IWebHostBuilder> (webHostBuilder endpoints))
        .Build()
        .Run()

/// The default host configuration
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

/// Start the default host
let startWebHostDefault =
    fun (args : string[]) 
        (endpoints : HttpEndpoint list) ->
            
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHost(Action<IWebHostBuilder> (defaultConfigureWebHost endpoints))
        .Build()
        .Run()
