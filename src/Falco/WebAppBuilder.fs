[<AutoOpen>]
module Falco.Builder

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

type WebApp = 
    {
        Configurations : IApplicationBuilder -> IApplicationBuilder
        HostBuilder    : IWebHostBuilder
        Logging        : ILoggingBuilder -> ILoggingBuilder
        NotFound       : HttpHandler option
        Routes         : HttpEndpoint list
        Services       : IServiceCollection -> IServiceCollection
    }

    static member Empty () = 
        { 
            Configurations = id
            HostBuilder    = WebHostBuilder().UseKestrel()
            Logging        = id
            NotFound       = None
            Routes         = []
            Services       = id
        }

type WebAppBuilder() =    
    member __.Yield(_) = WebApp.Empty ()

    member __.Run(webApp : WebApp) =          
        let host = webApp.HostBuilder
        host.ConfigureLogging(fun logging -> 
                logging
                |> webApp.Logging
                |> ignore)
            .ConfigureServices(fun services -> 
                services.AddRouting() 
                |> webApp.Services 
                |> ignore)
            .Configure(fun app ->
                app.UseRouting().UseHttpEndPoints(webApp.Routes)
                |> webApp.Configurations
                |> fun _ -> 
                    match webApp.NotFound with
                    | Some nf -> app.UseNotFoundHandler(nf)
                    | None _ -> ())
        |> ignore

        host.Build().Run()
    
    [<CustomOperation("configure")>]
    member __.Configure (app : WebApp, appConfig : IApplicationBuilder -> IApplicationBuilder) =
        { app with Configurations = app.Configurations >> appConfig }


    [<CustomOperation("logging")>]
    member __.Logging (app : WebApp, logConfig : ILoggingBuilder -> ILoggingBuilder) =
        { app with Logging = app.Logging >> logConfig }

    [<CustomOperation("services")>]
    member __.Services (app : WebApp, servicesConfig : IServiceCollection -> IServiceCollection) =
        { app with Services = app.Services >> servicesConfig }

    [<CustomOperation("route")>]
    member __.Route (app : WebApp, verb : HttpVerb, pattern : string, handler : HttpHandler) =
        { app with Routes = route verb pattern handler :: app.Routes }

    [<CustomOperation("any")>]
    member __.Any (app : WebApp, pattern : string, handler : HttpHandler) =
        
        { app with Routes = any pattern handler :: app.Routes }
    
    [<CustomOperation("get")>]
    member __.Get (app : WebApp, pattern : string, handler : HttpHandler) =
        { app with Routes = get pattern handler :: app.Routes }
    
    [<CustomOperation("head")>]
    member __.Head (app : WebApp, pattern : string, handler : HttpHandler) =
        { app with Routes = head pattern handler :: app.Routes }
    
    [<CustomOperation("post")>]
    member __.Post (app : WebApp, pattern : string, handler : HttpHandler) =
        { app with Routes = post pattern handler :: app.Routes }
    
    [<CustomOperation("put")>]
    member __.Put (app : WebApp, pattern : string, handler : HttpHandler) =
        { app with Routes = put pattern handler :: app.Routes }
    
    [<CustomOperation("patch")>]
    member __.Patch (app : WebApp, pattern : string, handler : HttpHandler) =
        { app with Routes = patch pattern handler :: app.Routes }
    
    [<CustomOperation("delete")>]
    member __.Delete (app : WebApp, pattern : string, handler : HttpHandler) =
        { app with Routes = delete pattern handler :: app.Routes }
    
    [<CustomOperation("options")>]
    member __.Options (app : WebApp, pattern : string, handler : HttpHandler) =
        { app with Routes = options pattern handler :: app.Routes }
    
    [<CustomOperation("trace")>]
    member __.Trace (app : WebApp, pattern : string, handler : HttpHandler) =
        { app with Routes = trace pattern handler :: app.Routes }

    [<CustomOperation("notFound")>]
    member __.NotFound (app : WebApp, handler : HttpHandler) =
        { app with NotFound = Some handler }

let webApp = WebAppBuilder()

