module AuthExample.Program

open System
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.IdentityModel.Tokens
open System.Security.Claims

// ------------
// Entities
// ------------
type User =
    { Id        : string
      Username  : string
      Name      : string
      Surname   : string }

type UserDto =
    { Username  : string
      Name      : string
      Surname   : string }

type Error =
    { Code      : string
      Message   : string }

// ------------
// Storage
// ------------
type IStorage =
    abstract member GetAll : unit   -> Result<User seq, Error>
    abstract member Add    : string -> UserDto -> Result<User, Error>
    abstract member Update : string -> UserDto -> Result<User, Error>
    abstract member Remove : string -> Result<User, Error>

type MemoryStorage() =
    let mutable values = [
        { Id = "d19bc3e4-4b72-488b-a739-df812bd892c9"; Username = "user1"; Name = "John"; Surname = "Doe" }
        { Id = "11beebd6-6a0b-42f7-bf70-56b168cdd55c"; Username = "user2"; Name = "Mario"; Surname = "Rossi" }
        { Id = "096527f3-ceed-4d27-bcd9-d4c1da7798ab"; Username = "user3"; Name = "Stephen"; Surname = "Knight" }
    ]
    interface IStorage with
        member _.GetAll() =
            values |> Seq.map id |> Result.Ok
        member _.Add(id : string) (userDto : UserDto) =
            let user = { Id = id; Username = userDto.Username; Name = userDto.Name; Surname = userDto.Surname }
            values <- List.append values [user]
            Result.Ok user
        member _.Update(id: string) (userDto: UserDto) =
            let user = { Id = id; Username = userDto.Username; Name = userDto.Name; Surname = userDto.Surname }
            values <- values |> List.map (fun u -> if u.Id = id then user else u)
            Result.Ok user
        member _.Remove(id: string) =
            let user = values |> List.find (fun u -> u.Id = id)
            values <- values |> List.filter (fun u -> u.Id <> id)
            Result.Ok user

// ------------
// User Storage
// ------------
module UserStorage =
    let getAll (storage : IStorage) () =
        storage.GetAll()

    let create (storage : IStorage) (userDto : UserDto) =
        let id = Guid.NewGuid().ToString()
        storage.Add id userDto

    let update (storage : IStorage) (id : string) (userDto : UserDto) =
        let checkUserExist users =
            users
            |> Seq.tryFind (fun user -> user.Id = id)
            |> function
                | Some user -> Result.Ok user
                | None      -> Result.Error { Code = "123"; Message = "User to update not found!" }

        storage.GetAll()
        |> Result.bind checkUserExist
        |> Result.bind (fun _ -> storage.Update id userDto)

    let delete (storage : IStorage) (id : string) =
        let checkUserExist users =
            users
            |> Seq.tryFind (fun user -> user.Id = id)
            |> function
                | Some user -> Result.Ok user
                | None      -> Result.Error { Code = "456"; Message = "User to delete not found!" }

        storage.GetAll()
        |> Result.bind checkUserExist
        |> Result.bind (fun _ -> storage.Remove id)

// ------------
// Haandlers
// ------------
module ErrorPages =
    let unauthorized : HttpHandler =
        Response.withStatusCode 401
        >> Response.ofPlainText "Unauthorized"

    let forbidden : HttpHandler =
        Response.withStatusCode 403
        >> Response.ofPlainText "Forbidden"

    let badRequest : HttpHandler =
        Response.withStatusCode 400
        >> Response.ofPlainText "Bad request"

    let serverError : HttpHandler =
        Response.withStatusCode 500
        >> Response.ofPlainText "Server Error"

module Middleware =
    let withStorage (next : IStorage -> HttpHandler) : HttpHandler = fun ctx ->
        let stoage = ctx.RequestServices.GetRequiredService<IStorage>()
        next stoage ctx

module UserHandlers =
    open Middleware

    let private jsonError json : HttpHandler =
        Response.withStatusCode 400 >> Response.ofJson json

    let private handleResult result =
        match result with
        | Ok result   -> Response.ofJson result
        | Error error -> jsonError error

    let index : HttpHandler =
        Response.ofPlainText "Hello World, by Falco"

    let create : HttpHandler =
        withStorage (fun storage ->
            Request.mapJson (fun json ->
                handleResult (UserStorage.create storage json)))

    let readAll : HttpHandler =
        withStorage (fun storage ->
            handleResult (UserStorage.getAll storage ()))

    let private idFromRoute (r : RouteCollectionReader) =
        r.GetString "id" ""

    let update : HttpHandler =
        withStorage (fun storage ->
            Request.mapRoute idFromRoute (fun id ->
                Request.mapJson (fun (userDto : UserDto) ->
                    handleResult (UserStorage.update storage id userDto))))

    let delete : HttpHandler =
        withStorage (fun storage ->
            Request.mapRoute idFromRoute (fun id ->
                handleResult (UserStorage.delete storage id)))

// ------------
// Auth
// ------------
module AuthConfig =
    let authority = "https://falco-auth-test.us.auth0.com/"
    let audience = "https://users/api"

    let createUsersPolicy = "create:users"
    let readUsersPolicy = "read:users"
    let updateUsersPolicy = "update:users"
    let deleteUsersPolicy = "delete:users"

module Auth =
    let hasScope (scope : string) (next : HttpHandler) : HttpHandler =
        Request.ifAuthenticatedWithScope AuthConfig.authority scope next ErrorPages.forbidden

// ------------
// Register services
// ------------
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

webHost [||] {
    add_service authService
    add_service memoryStorageService

    use_ifnot FalcoExtensions.IsDevelopment (FalcoExtensions.UseFalcoExceptionHandler ErrorPages.serverError)
    use_authentication

    endpoints [
        get "/" UserHandlers.index

        get "/users" (Auth.hasScope AuthConfig.readUsersPolicy UserHandlers.readAll)

        post "/users" (Auth.hasScope AuthConfig.createUsersPolicy UserHandlers.create)

        put "/users/{id:guid}" (Auth.hasScope AuthConfig.updateUsersPolicy UserHandlers.update)

        delete "/users/{id:guid}" (Auth.hasScope AuthConfig.deleteUsersPolicy UserHandlers.delete)
    ]
}