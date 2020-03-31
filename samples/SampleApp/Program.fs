module SampleApp 

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Falco
open Falco.Auth
open Falco.ViewEngine

// ------------
// Handlers
// ------------
let helloHandler : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->        
        let name = ctx.RouteValue "name" |> Option.defaultValue "someone"
        textOut (sprintf "hi %s" name) next ctx

let myHtmlOutHandler : HttpHandler =
    let myHtml = html [ _lang "en" ] [
            head [] [
                meta  [ _charset "UTF-8" ]
                meta  [ _httpEquiv "X-UA-Compatible"; _content "IE=edge,chrome=1" ]
                meta  [ _name "viewport"; _content "width=device-width,initial-scale=1" ]
                title [] [ raw "Sample App" ]                                        
                link  [ _href "/style.css"; _rel "stylesheet"]
            ]
            body [] [                     
                    main [] [
                            h1 [] [ raw "Sample App" ]
                        ]
                ]
        ] 

    htmlOut myHtml

type Person =
    {
        First : string
        Last  : string 
    }

let myJsonHandler : HttpHandler =
    jsonOut { First = "Pim"; Last = "Brouwers" }
   
// ------------
// Web App
// ------------
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
        get   "/secure"             (ifAuthenticated (redirect "/forbidden" false) >=> textOut "hello authenticated person")
        get   "/html"               myHtmlOutHandler
        get   "/json"               myJsonHandler
        get   "/hello/{name:alpha}" helloHandler
        get   "/forbidden"          (setStatusCode 403 >=> textOut "Forbidden")
        route "/"                   (textOut "index")
    ]

    app.UseDeveloperExceptionPage()
       .UseStaticFiles()
       .UseHttpEndPoints(routes)
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