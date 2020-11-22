module AppName.Program

open Falco
open Falco.Routing
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

// ------------
// Handlers 
// ------------
let handlePlainText : HttpHandler =
    Response.ofPlainText "Hello from /"

// ------------
// Routes
// ------------
let endpoints =
    [            
        get "/" handlePlainText
    ]

// ------------
// Register services
// ------------
let configureServices (services : IServiceCollection) =
    services.AddFalco() |> ignore

// ------------
// Activate middleware
// ------------
let configureApp (ctx : WebHostBuilderContext) (app : IApplicationBuilder) =    
    let devMode = StringUtils.strEquals ctx.HostingEnvironment.EnvironmentName "Development"    
    app.UseWhen(devMode, fun app -> 
            app.UseDeveloperExceptionPage())
       .UseWhen(not(devMode), fun app -> 
            app.UseFalcoExceptionHandler(Response.withStatusCode 500 >> Response.ofPlainText "Server error"))
       .UseFalco(endpoints) |> ignore

[<EntryPoint>]
let main args =    
    try
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(fun webhost ->   
                webhost
                    .ConfigureServices(configureServices)
                    .Configure(configureApp)
                    |> ignore)
            .Build()
            .Run()                        
        0
    with 
    | ex -> 
        printfn "%s\n\n%s" ex.Message ex.StackTrace
        -1