module Falco.HostBuilder

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

/// Represents the eventual existence of a runnable IWebhost
type HostSpec = 
    {        
        Builder   : HttpEndpoint list -> IWebHostBuilder -> IWebHostBuilder        
        Endpoints : HttpEndpoint list            
    }
    static member Empty() = 
        let defaultBuilder (endpoints : HttpEndpoint list) (webHost : IWebHostBuilder) =
            let configureServices (services : IServiceCollection) =
                services.AddFalco() |> ignore

            let configureApp (app : IApplicationBuilder) =                
                app.UseDeveloperExceptionPage()
                   .UseFalco(endpoints) |> ignore

            webHost.ConfigureServices(configureServices).Configure(configureApp)

        { 
            Builder       = defaultBuilder
            Endpoints     = []               
        }

/// Computation expression to allow for elegant IHost construction
type HostBuilder(args : string[]) =    
    member __.Yield(_) = HostSpec.Empty ()

    member __.Run(webHost : HostSpec) =              
        let configure = 
            let wrappedBuilder = fun bldr -> webHost.Builder webHost.Endpoints bldr |> ignore
            Action<IWebHostBuilder>(wrappedBuilder)

        Host.CreateDefaultBuilder(args)                 
            .ConfigureWebHostDefaults(configure)
            .Build()
            .Run()   
    
    /// Configure the IWebHost
    [<CustomOperation("configure")>]
    member __.Configure (spec : HostSpec, builder : HttpEndpoint list -> IWebHostBuilder -> IWebHostBuilder) =
        { spec with Builder = builder }

    /// Falco HttpEndpoint's
    [<CustomOperation("endpoints")>]
    member __.Endpoints (webHost : HostSpec, endpoints : HttpEndpoint list) =
        { webHost with Endpoints = endpoints }

/// A computation expression to make IHost construction easier 
let webHost args = HostBuilder(args)

