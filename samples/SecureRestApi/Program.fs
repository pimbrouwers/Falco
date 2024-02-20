module AuthExample.Program

open System
open Falco
open Falco.Routing
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.IdentityModel.Tokens
open System.Security.Claims

module Domain = 
    type Error =
        { Code      : string
          Message   : string }
        
    module UserModel = 
        type User =
            { Id        : string
              Username  : string
              Name      : string
              Surname   : string }

        type UserDto =
            { Username  : string
              Name      : string
              Surname   : string }

module Infrastructure = 
    open System.Collections.Generic
    open Domain 
    open Domain.UserModel 
    
    type IUserStorage =
        abstract member GetAll : unit -> Result<User seq, Error>
        abstract member Add : string -> UserDto -> Result<User, Error>
        abstract member Update : string -> UserDto -> Result<User, Error>
        abstract member Remove : string -> Result<User, Error>

    type UserMemoryStorage(initialValues : User seq) =
        let mutable store = 
            Dictionary<string, User>(initialValues |> Seq.map (fun x -> x.Id, x) |> Map.ofSeq)
        
        interface IUserStorage with
            member _.GetAll() =
                Result.Ok store.Values
            
            member _.Add(id : string) (userDto : UserDto) =
                if not(store.ContainsKey(id)) then
                    let user = { Id = id; Username = userDto.Username; Name = userDto.Name; Surname = userDto.Surname }
                    store.Add(id, user) |> ignore
                    Result.Ok user
                else
                    Result.Error { Code = "Not found"; Message = "Could not add user" }
                
            member _.Update(id: string) (userDto: UserDto) =
                let user = { Id = id; Username = userDto.Username; Name = userDto.Name; Surname = userDto.Surname }
                if store.ContainsKey(id) then 
                    store[id] <- user
                    Result.Ok user
                else
                    Result.Error { Code = "Not found"; Message = "Could not update user" }
                
            member _.Remove(id: string) =
                if store.ContainsKey(id) then
                    let user = store[id]
                    store.Remove(id) |> ignore
                    Result.Ok user
                else
                    Result.Error { Code = "Not found"; Message = "Could not remove user" }

module Service = 
    open Domain
    open Infrastructure

    module UserService =
        open Domain.UserModel

        let getAll (storage : IUserStorage) () =
            storage.GetAll()

        let create (storage : IUserStorage) (userDto : UserDto) =
            let id = Guid.NewGuid().ToString()
            storage.Add id userDto

        let update (storage : IUserStorage) (id : string) (userDto : UserDto) =
            let checkUserExist users =
                users
                |> Seq.tryFind (fun user -> user.Id = id)
                |> function
                    | Some user -> Result.Ok user
                    | None      -> Result.Error { Code = "123"; Message = "User to update not found!" }

            storage.GetAll()
            |> Result.bind checkUserExist
            |> Result.bind (fun _ -> storage.Update id userDto)

        let delete (storage : IUserStorage) (id : string) =
            let checkUserExist users =
                users
                |> Seq.tryFind (fun user -> user.Id = id)
                |> function
                    | Some user -> Result.Ok user
                    | None      -> Result.Error { Code = "456"; Message = "User to delete not found!" }

            storage.GetAll()
            |> Result.bind checkUserExist
            |> Result.bind (fun _ -> storage.Remove id)

module Web =
    open Domain
    open Infrastructure
    open Service

    module ErrorController =
        let badRequest : HttpHandler =
            Response.withStatusCode 400
            >> Response.ofPlainText "Bad Request"
        
        let unauthorized : HttpHandler =
            Response.withStatusCode 401
            >> Response.ofPlainText "Unauthorized"

        let forbidden : HttpHandler =
            Response.withStatusCode 403
            >> Response.ofPlainText "Forbidden"

        let notFound : HttpHandler =
            Response.withStatusCode 404
            >> Response.ofPlainText "Not Found"

        let serverError : HttpHandler =
            Response.withStatusCode 500
            >> Response.ofPlainText "Server Error"

    module UserController =
        open Domain.UserModel

        let private jsonError json : HttpHandler =
            Response.withStatusCode 400 >> Response.ofJson json

        let private handleResult result =
            match result with
            | Ok result   -> Response.ofJson result
            | Error error -> jsonError error

        let index : HttpHandler =
            Response.ofPlainText "Hello World, by Falco"

        let create (storage : IUserStorage): HttpHandler =
            Request.mapJson (fun json ->
                handleResult (UserService.create storage json))

        let readAll (storage : IUserStorage): HttpHandler =
            handleResult (UserService.getAll storage ())

        let private idFromRoute (r : RouteCollectionReader) =
            r.GetString "id"

        let update (storage : IUserStorage): HttpHandler =
            Request.mapRoute idFromRoute (fun id ->
                Request.mapJson (fun (userDto : UserDto) ->
                    handleResult (UserService.update storage id userDto)))

        let delete (storage : IUserStorage): HttpHandler =
            Request.mapRoute idFromRoute (fun id ->
                handleResult (UserService.delete storage id))

module Program = 
    open Falco
    open Falco.Routing
    open Infrastructure
    open Web

    type App(userStorage : IUserStorage) =
        let authority = "https://falco-auth-test.us.auth0.com/"
        let audience = "https://users/api"

        let createUsersPolicy = "create:users"
        let readUsersPolicy = "read:users"
        let updateUsersPolicy = "update:users"
        let deleteUsersPolicy = "delete:users"

        let hasScope (scope : string) (next : HttpHandler) : HttpHandler =
            Request.ifAuthenticatedWithScope authority scope next ErrorController.forbidden

        member _.Auth =
            {| Authority = authority; Audience = audience |}

        member _.Endpoints = seq {
            get "/" UserController.index
            get "/users" (hasScope readUsersPolicy (UserController.readAll userStorage))
            post "/users" (hasScope createUsersPolicy (UserController.create userStorage))
            put "/users/{id:guid}" (hasScope updateUsersPolicy (UserController.update userStorage))
            delete "/users/{id:guid}" (hasScope deleteUsersPolicy (UserController.delete userStorage))
        }

        member _.NotFound = ErrorController.notFound

    [<EntryPoint>]
    let main args = 
        let app =
            let userStorage = UserMemoryStorage(Seq.empty) 
            App(userStorage)

        let bldr = WebApplication.CreateBuilder(args)
            
        bldr.Services            
            .AddAuthentication(fun options ->         
                options.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
                options.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(fun options ->
                    let createTokenValidationParameters () =
                        let tvp = new TokenValidationParameters()
                        tvp.NameClaimType <- ClaimTypes.NameIdentifier
                        tvp
                    options.Authority <- app.Auth.Authority
                    options.Audience <- app.Auth.Audience
                    options.TokenValidationParameters <- createTokenValidationParameters())
            |> ignore    

        let wapp = bldr.Build()
        wapp.UseFalco(app.Endpoints)
            .Run(app.NotFound)
            |> ignore

        wapp.Run()
        0
