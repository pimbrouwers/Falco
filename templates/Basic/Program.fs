module AppName.Program

open Falco
open Falco.Routing
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

// ------------
// Routes
// ------------
let endpoints =
    [            
        get "/" (Response.ofPlainText "Hello world")
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

let mapQueryHandler : HttpHandler =    
    Request.mapRoute
        (fun route -> route.GetString "Name" "John Doe")
        Response.ofJson 

let bindQueryHandler : HttpHandler = 
    Request.bindQuery 
        (fun route -> 
            match route.TryGetString "Name" with
            | Some name -> Ok name
            | _         -> Error {| Message = "Invalid route" |})
        Response.ofJson // handle Ok
        Response.ofJson // handle Error

let manualQueryHandler : HttpHandler =
    fun ctx ->
        let route = Request.getRoute ctx
        let name = route.GetString "Name" "John Doe"
        Response.ofJson name ctx

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