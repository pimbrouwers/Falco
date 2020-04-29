module SampleApp.Program

open System
open System.IO
open Falco
open Falco.Auth
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open SampleApp.Handlers

let tryGetEnv = Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x

let env = tryGetEnv "ASPNETCORE_ENVIRONMENT"
let isDev = match env with None -> true | Some env -> strEquals env "development"

let root = tryGetEnv WebHostDefaults.ContentRootKey |> Option.defaultValue (Directory.GetCurrentDirectory())

webApp {    
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
    notFound                   (setStatusCode 404 >=> textOut "Not found")

    host       (fun hst -> hst.UseContentRoot(root))

    configure  (fun cnf -> cnf.SetBasePath(root)
                              .AddJsonFile("appsettings.json", false))

    errors     (fun ex _ -> setStatusCode 500 >=> textOut (sprintf "Error: %s" ex.Message))

    logging    (fun log -> log.AddConsole()
                              .AddDebug())

    services   (fun svc -> svc.AddResponseCompression()
                              .AddResponseCaching())

    middleware (fun app -> 
                    if isDev then app.UseDeveloperExceptionPage() |> ignore
                    app.UseStaticFiles())    
}