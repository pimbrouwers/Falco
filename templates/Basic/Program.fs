module AppName.Program

open Falco
open Falco.Markup
open Falco.Routing
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

let endpoints =
    [            
        get "/" (Response.ofPlainText "Hello world")
    ]

let configureServices (services : IServiceCollection) =
    services.AddFalco() |> ignore

let configureApp (ctx : WebHostBuilderContext) (app : IApplicationBuilder) =    
    let env = ctx.HostingEnvironment.EnvironmentName
    let developerMode = StringUtils.strEquals env "Development"
    
    app.UseWhen(developerMode, fun app -> 
            app.UseDeveloperExceptionPage())
       .UseWhen(not(developerMode), fun app -> 
            app.UseFalcoExceptionHandler(Response.withStatusCode 500 >> Response.ofPlainText "Server Error"))
       .UseFalco(endpoints) 
       |> ignore

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