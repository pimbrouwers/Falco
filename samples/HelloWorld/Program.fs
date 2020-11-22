module HelloWorld.Program

open Falco
open Falco.Markup
open Falco.Routing
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

let endpoints = 
    [            
        get "/greet/{name:alpha}"
            (Request.mapRoute (fun r -> r.GetString "name" "John Doe" |> sprintf "Hi %s") Response.ofPlainText)

        get "/json" 
            (Response.ofJson {| Message = "Hello from /json" |})

        get "/html" 
            (Response.ofHtml (Templates.html5 "en" [] [ Elem.h1 [] [ Text.raw "Hello from /html" ] ]))

        get "/" 
            (Response.ofPlainText "Hello from /")
    ]

let configureServices (services : IServiceCollection) =
    services.AddFalco() |> ignore

let configureApp (ctx : WebHostBuilderContext) (app : IApplicationBuilder) =    
    let env = ctx.HostingEnvironment.EnvironmentName
    let developerMode = StringUtils.strEquals env "Development"
    
    app.UseWhen(developerMode, fun app -> app.UseDeveloperExceptionPage())
       .UseWhen(not(developerMode), fun app -> app.UseFalcoExceptionHandler(Response.withStatusCode 500 >> Response.ofPlainText "Server Error"))
       .UseFalco(endpoints) 
       |> ignore

[<EntryPoint>]
let main args =    
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(fun webhost ->   
            webhost
                .ConfigureServices(configureServices)
                .Configure(configureApp)
                |> ignore)
        .Build()
        .Run()                        
    0