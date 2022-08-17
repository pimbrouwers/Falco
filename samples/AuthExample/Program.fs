module AuthExample.Program

open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.IdentityModel.Tokens
open System.Security.Claims
open AuthExample

module Router =
    let endpoints =
        [
            get "/" UserHandlers.index

            get "/users" (Auth.hasScope AuthConfig.readUsersPolicy UserHandlers.readAll)

            post "/users" (Auth.hasScope AuthConfig.createUsersPolicy UserHandlers.create)

            put "/users/{id:guid}" (Auth.hasScope AuthConfig.updateUsersPolicy UserHandlers.update)

            delete "/users/{id:guid}" (Auth.hasScope AuthConfig.deleteUsersPolicy UserHandlers.delete)
        ]

[<EntryPoint>]
let main args =
    let authService (svc : IServiceCollection) =
        let createTokenValidationParameters () =
            let tvp = new TokenValidationParameters()
            tvp.NameClaimType <- ClaimTypes.NameIdentifier
            tvp

        svc.AddAuthentication(fun options ->
            options.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
            options.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(fun options ->
                options.Authority <- AuthConfig.authority
                options.Audience <- AuthConfig.audience
                options.TokenValidationParameters <- createTokenValidationParameters()) |> ignore

        svc

    let memoryStorageService (svc : IServiceCollection) =
        svc.AddSingleton<IStorage, MemoryStorage>(fun _ -> MemoryStorage())

    webHost args {
        add_service authService
        add_service memoryStorageService

        use_ifnot FalcoExtensions.IsDevelopment (FalcoExtensions.UseFalcoExceptionHandler ErrorPages.serverError)
        use_authentication

        endpoints Router.endpoints
    }

    0