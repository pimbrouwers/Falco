module SampleApp.Program

open System.IO
open Falco
open Falco.Auth
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open SampleApp.Handlers

let root = Directory.GetCurrentDirectory()

webApp {
    contentRoot root
    logging     (fun log -> log.AddConsole().AddDebug())
    services    (fun svc -> svc.AddResponseCompression().AddResponseCaching())
    configure   (fun app -> app.UseDeveloperExceptionPage().UseStaticFiles())

    get  "/search"             searchViewHandler
    get  "/search-results"     searchResultsHandler
    get  "/new-user"           newUserViewHandler
    post "/new-user"           newUserHandler
    get  "/json"               myJsonOutHandler
    get  "/html"               myHtmlOutHandler
    get  "/secure"             (ifAuthenticated (redirect "/forbidden" false) >=> textOut "hello authenticated person")
    get  "/forbidden"          (setStatusCode 403 >=> textOut "Forbidden")
    get  "/hello/{name:alpha}" helloHandler
    any  "/"                   (textOut "index")

    notFound notFoundHandler
}

//// ------------
//// Routes
//// ------------
//let routes = [
//    get  "/search"             searchViewHandler
//    get  "/search-results"     searchResultsHandler
//    get  "/new-user"           newUserViewHandler
//    post "/new-user"           newUserHandler
//    get  "/json"               myJsonOutHandler
//    get  "/html"               myHtmlOutHandler
//    get  "/secure"             (ifAuthenticated (redirect "/forbidden" false) >=> textOut "hello authenticated person")
//    get  "/forbidden"          (setStatusCode 403 >=> textOut "Forbidden")
//    get  "/hello/{name:alpha}" helloHandler
//    any  "/"                   (textOut "index")
//]

//// ------------
//// Web App
//// ------------
//let configureApp (app : IApplicationBuilder) =          
//    app.UseDeveloperExceptionPage()
//       .UseStaticFiles()
//       .UseRouting()
//       .UseHttpEndPoints(routes)
//       .UseNotFoundHandler(notFoundHandler)
//       |> ignore

//// ------------
//// Logging
//// ------------
//let configureLogging (loggerBuilder : ILoggingBuilder) =
//    loggerBuilder
//        .AddFilter(fun l -> l.Equals LogLevel.Error)
//        .AddConsole()
//        .AddDebug() |> ignore

//// ------------
//// Services
//// ------------
//let configureServices (services : IServiceCollection) =
//    services
//        .AddResponseCaching()
//        .AddResponseCompression()            
//        .AddRouting()
//        |> ignore

//[<EntryPoint>]
//let main _ =
//    try
//        WebHostBuilder()
//            .UseKestrel()       
//            .ConfigureLogging(configureLogging)
//            .ConfigureServices(configureServices)
//            .Configure(configureApp)          
//            .UseContentRoot(Directory.GetCurrentDirectory())
//            .Build()
//            .Run()
//        0
//    with 
//        | _ -> -1