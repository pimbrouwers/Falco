module Blog.Server

open System
open Falco    
open Falco.Host
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting    
open Microsoft.Extensions.DependencyInjection  
open Microsoft.Extensions.Logging

let handleException 
    (developerMode : DeveloperMode) : ExceptionHandler =
    let (DeveloperMode developerMode) = developerMode

    fun (ex : Exception)
        (log : ILogger) ->
        let logMessage = 
            match developerMode with
            | true  -> sprintf "Server error: %s\n\n%s" ex.Message ex.StackTrace
            | false -> "Server Error"
        
        log.Log(LogLevel.Error, logMessage)        
        
        Response.withStatusCode 500
        >> Response.ofPlainText logMessage
    
let handleNotFound : HttpHandler = 
    Response.withStatusCode 404
    >> Response.ofPlainText "Not found"
    
let configureWebHost 
    (developerMode : DeveloperMode) : ConfigureWebHost =            
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
        (developerMode : DeveloperMode)
        (enpoints : HttpEndpoint list)
        (app : IApplicationBuilder) = 
            
        app.UseExceptionMiddleware(handleException developerMode)
            .UseResponseCaching()
            .UseResponseCompression()
            .UseStaticFiles()
            .UseRouting()
            .UseHttpEndPoints(enpoints)
            .UseNotFoundHandler(handleNotFound)
            |> ignore 

    fun (endpoints : HttpEndpoint list)
        (webHost : IWebHostBuilder) ->                              
        webHost
            .UseKestrel()
            .ConfigureLogging(configureLogging)
            .ConfigureServices(configureServices)
            .Configure(configure developerMode endpoints)
            |> ignore