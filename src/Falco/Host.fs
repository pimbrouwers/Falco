module Falco.Host

open System    
open Falco    
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting    
open Microsoft.Extensions.DependencyInjection    
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

type BuildHost = IWebHostBuilder -> unit

let defaultExceptionHandler 
    (ex : Exception)
    (log : ILogger) : HttpHandler =
    fun ctx ->   
        ctx.Response.SetStatusCode 500

        let logMessage =
            sprintf "Server error: %s\n\n%s" ex.Message ex.StackTrace
        log.Log(LogLevel.Error, logMessage)
        Response.ofPlainText ctx logMessage 
        
let defaultNotFoundHandler : HttpHandler =
    fun ctx ->
        ctx.Response.SetStatusCode 404        
        Response.ofPlainText ctx "Not found" 

let startDefaultHost =
    fun (args : string[]) 
        (endpoints : HttpEndpoint list) ->

    let configureWebHost : BuildHost =
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

        fun (webHost : IWebHostBuilder) ->                       
            webHost
                .UseKestrel()
                .ConfigureLogging(configureLogging)
                .ConfigureServices(configureServices)
                .Configure(configure endpoints)
                |> ignore

    Host.CreateDefaultBuilder(args)
        .ConfigureWebHost(Action<IWebHostBuilder> configureWebHost)
        .Build()
        .Run()
