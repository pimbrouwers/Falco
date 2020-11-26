module AppName.Program

open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection

// ------------
// Register services
// ------------
let configureServices (services : IServiceCollection) =
    services.AddFalco() |> ignore

// ------------
// Activate middleware
// ------------
let configureApp (endpoints : HttpEndpoint list) (ctx : WebHostBuilderContext) (app : IApplicationBuilder) =    
    let devMode = StringUtils.strEquals ctx.HostingEnvironment.EnvironmentName "Development"    
    app.UseWhen(devMode, fun app -> 
            app.UseDeveloperExceptionPage())
       .UseWhen(not(devMode), fun app -> 
            app.UseFalcoExceptionHandler(Response.withStatusCode 500 >> Response.ofPlainText "Server error"))
       .UseFalco(endpoints) |> ignore

// -----------
// Configure Web host
// -----------
let configureWebHost (endpoints : HttpEndpoint list) (webHost : IWebHostBuilder) =
    webHost
        .ConfigureServices(configureServices)
        .Configure(configureApp endpoints)

[<EntryPoint>]
let main args =   
    webHost args {
        configure configureWebHost
        endpoints [            
            get "/" (Response.ofPlainText "Hello world")
        ]
    }
    0