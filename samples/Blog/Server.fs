module Blog.Server

open System
open Falco    
open Falco.Host
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting 
open Microsoft.Extensions.DependencyInjection  
open Microsoft.Extensions.Logging

type ConfigureLogging = DeveloperMode -> ILoggingBuilder -> unit
type ConfigureServices = IServiceCollection -> unit
type ConfigureApp = DeveloperMode -> HttpEndpoint list -> IApplicationBuilder -> unit
type ConfigureServer = ContentRoot -> DeveloperMode -> ConfigureWebHost

let handleException 
    (developerMode : DeveloperMode) : ExceptionHandler =
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
    Response.withStatusCode 404 >> Response.ofHtml UI.notFound

let configureLogging : ConfigureLogging =
    fun devMode log ->
        log.AddFilter(fun l -> l >= (if devMode then LogLevel.Warning else LogLevel.Error))
           |> ignore

let configureServices : ConfigureServices =
    fun services ->
        services.AddRouting()     
                .AddResponseCaching()
                .AddResponseCompression()
                |> ignore        

let configureApp : ConfigureApp =
    fun devMode endpoints app ->
        app.UseExceptionMiddleware(handleException devMode)
           .UseResponseCaching()
           .UseResponseCompression()
           .UseStaticFiles()
           .UseRouting()
           .UseHttpEndPoints(endpoints)
           .UseNotFoundHandler(handleNotFound)
           |> ignore 

let configure : ConfigureServer =
    fun contentRoot devMode endpoints webhost ->
        webhost.UseKestrel()               
               .UseContentRoot(contentRoot)
               .ConfigureLogging(configureLogging devMode)
               .ConfigureServices(configureServices)
               .Configure(configureApp devMode endpoints)
               |> ignore

    