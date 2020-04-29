module SampleApp.Program

open Falco
open Falco.Auth
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open SampleApp.Handlers

webApp {    
    logging     (fun log -> log.AddConsole().AddDebug())
    services    (fun svc -> svc.AddResponseCompression().AddResponseCaching())
    configure   (fun app -> app.UseStaticFiles())

    get  "/throw-exception"    exceptionThrowingHandler
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

    notFound (setStatusCode 404 >=> textOut "Not found")

    errors (fun ex _ -> setStatusCode 500 >=> textOut (sprintf "Error: %s" ex.Message))
}