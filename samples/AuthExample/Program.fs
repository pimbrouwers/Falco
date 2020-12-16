module AuthExample.Program

open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.IdentityModel.Tokens
open System.Security.Claims
open AuthExample.Handler
open AuthExample.AuthConfig

let configureServices (services : IServiceCollection) =
    let createTokenValidationParameters () =
        let tvp = new TokenValidationParameters()
        tvp.NameClaimType <- ClaimTypes.NameIdentifier
        tvp

    services.AddAuthentication(fun options ->
                options.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
                options.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme
            )
            .AddJwtBearer(fun options ->
                options.Authority <- authority
                options.Audience <- audience
                options.TokenValidationParameters <- createTokenValidationParameters()
            ) |> ignore
    services.AddFalco() |> ignore

let configureApp (endpoints : HttpEndpoint list) (app : IApplicationBuilder) =    
    app.UseAuthentication()
       .UseFalco(endpoints) |> ignore

let configureWebhost (endpoints : HttpEndpoint list) (webhost : IWebHostBuilder) =
    webhost.ConfigureServices(configureServices)
           .Configure(configureApp endpoints)

[<EntryPoint>]
let main args =        
    try
        webHost args {
            configure configureWebhost

            endpoints [   
                get "/" 
                    handleIndex

                get "/users"
                    (Request.ifAuthenticatedWithScope readUsersPolicy authority handleReadUsers handleForbidden)

                post "/users"
                    (Request.ifAuthenticatedWithScope createUsersPolicy authority handleCreateUser handleForbidden)
                
                put "/users/{id:guid}"
                    (Request.ifAuthenticatedWithScope updateUsersPolicy authority handleUpdateUser handleForbidden)

                delete "/users/{id:guid}"
                    (Request.ifAuthenticatedWithScope deleteUsersPolicy authority handleDeleteUser handleForbidden)
            ]
        }           
        0
    with 
    | ex -> 
        printfn "%s\n\n%s" ex.Message ex.StackTrace
        -1