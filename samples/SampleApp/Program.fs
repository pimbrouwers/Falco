module SampleApp 

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Wildebeest

type Person =
    {
        First : string
        Last  : string 
    }

let myJsonHandler : HttpHandler =
    json { First = "Pim"; Last = "Brouwers" }

let helloHandler : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        let name = ctx.RouteValue "name" |> Option.defaultValue "someone"
        text (sprintf "hi %s" name) next ctx

let configureLogging (loggerBuilder : ILoggingBuilder) =
    loggerBuilder
        .AddFilter(fun l -> l.Equals LogLevel.Error)
        .AddConsole()
        .AddDebug() |> ignore

let configureServices (services : IServiceCollection) =
    services
        .AddResponseCaching()
        .AddResponseCompression()    
        .AddRouting()
        |> ignore

let configureApp (app : IApplicationBuilder) =      
    let routes = [
        get   "/json"               myJsonHandler
        get   "/hello/{name:alpha}" helloHandler
        route "/"                   (text "index")
    ]

    app.UseDeveloperExceptionPage()
       .UseHttpEndPoints(routes)
       |> ignore

[<EntryPoint>]
let main _ =
    try
        let host = new WebHostBuilder()
        host.UseKestrel()       
            .ConfigureLogging(configureLogging)
            .ConfigureServices(configureServices)
            .Configure(configureApp)          
            .Build()
            .Run()
        0
    with 
        | _ -> -1