module Blog.Program

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Falco
open Blog.Handlers

webApp {        
    get "/{slug:regex(^[a-z\-])}" blogPostHandler
    any "/"                       blogIndexHandler
    notFound                      (setStatusCode 404 >=> textOut "Not found")

    logging  (fun log -> log.AddConsole()
                            .AddDebug())

    services (fun svc -> svc.AddResponseCompression()
                            .AddResponseCaching())

    errors   (fun ex _ -> setStatusCode 500 >=> textOut (sprintf "Error: %s" ex.Message))
}