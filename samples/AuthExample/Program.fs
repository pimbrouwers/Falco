module HelloWorld.Program

open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.IdentityModel.Tokens
open System.Security.Claims
open System

// ------------
// Auth Config 
// ------------
let authority = "https://falco-auth-test.us.auth0.com/"
let audience = "https://users/api"

let createUsersPolicy = "create:users"
let readUsersPolicy = "read:users"
let updateUsersPolicy = "update:users"
let deleteUsersPolicy = "delete:users"

// ------------
// Domain 
// ------------
type User = 
    { Id        : string;
      Username  : string;
      Name      : string;
      Surname   : string; }
      
type UserDto = 
    { Username  : string;
      Name      : string;
      Surname   : string; }

type Error =
    { Code      : string
      Message   : string }

type IStorage =
    abstract member GetAll : unit -> Result<User seq, Error>
    abstract member Add : string -> UserDto -> Result<User, Error>
    abstract member Update : string -> UserDto -> Result<User, Error>
    abstract member Remove : string -> Result<User, Error>

let getAllUsers (storage : IStorage) () =
    storage.GetAll()

let createUser (storage : IStorage) (userDto : UserDto) =
    let id = Guid.NewGuid().ToString()
    storage.Add id userDto

let updateUser (storage : IStorage) (id : string) (userDto : UserDto) =
    let checkUserExist users =
        users
        |> Seq.tryFind (fun user -> user.Id = id)
        |> function
            | Some user -> Result.Ok user
            | None -> Result.Error { Code = "123"; Message = "User to update not found!" }
    storage.GetAll()
    |> Result.bind checkUserExist
    |> Result.bind (fun _ -> storage.Update id userDto)

let deleteUser (storage : IStorage) (id : string) =
    let checkUserExist users =
        users
        |> Seq.tryFind (fun user -> user.Id = id)
        |> function
            | Some user -> Result.Ok user
            | None -> Result.Error { Code = "456"; Message = "User to delete not found!" }
    storage.GetAll()
    |> Result.bind checkUserExist
    |> Result.bind (fun _ -> storage.Remove id)

// ------------
// Adapters
// ------------
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
// Init 
// ------------
let storage = MemoryStorage()

let getAllUsersWithStorage = getAllUsers storage
let createUserWithStorage = createUser storage
let updateUserWithStorage = updateUser storage
let deleteUserWithStorage = deleteUser storage

// ------------
// Handlers 
// ------------
let handleUnauthorized : HttpHandler = 
    Response.withStatusCode 401 
    >> Response.ofPlainText "Unauthorized"

let handleForbidden : HttpHandler = 
    Response.withStatusCode 403 
    >> Response.ofPlainText "Forbidden"

let handleBadRequest : HttpHandler =
    Response.withStatusCode 400 
    >> Response.ofPlainText "Bad request"

let handleError error : HttpHandler =
    Response.withStatusCode 400 
    >> Response.ofJson error

let handleResult = function
    | Result.Ok result -> Response.ofJson result
    | Result.Error error -> handleError error

let handleIndex : HttpHandler =
    "Hello World, by Falco"
    |> Response.ofPlainText

let handleCreateUser : HttpHandler =
    Request.bindJson
        (fun (userDto : UserDto) -> 
            createUserWithStorage userDto
            |> handleResult)
        (fun _ -> handleBadRequest)

let handleReadUsers : HttpHandler =
    Request.mapRoute
        (ignore)
        (fun _ -> 
            getAllUsersWithStorage ()
            |> handleResult)
    
let handleUpdateUser : HttpHandler =
    Request.bindRoute
        (fun routeCollection -> 
            routeCollection.TryGetString "id"
            |> function 
                | Some id -> Result.Ok id
                | None -> Result.Error "No user id provided")
        (fun id ->
            Request.bindJson
                (fun (userDto : UserDto) -> 
                    updateUserWithStorage id userDto
                    |> handleResult)
                (fun _ -> handleBadRequest))
        (fun _ -> handleBadRequest)
    
let handleDeleteUser : HttpHandler =
    Request.bindRoute
        (fun routeCollection -> 
            routeCollection.TryGetString "id"
            |> function 
                | Some id -> Result.Ok id
                | None -> Result.Error "No user id provided")
        (fun id ->
            deleteUserWithStorage id
            |> handleResult)
        (fun _ -> handleBadRequest)

let handleAuthCreateUser : HttpHandler =
    Request.ifAuthenticatedWithScope createUsersPolicy authority handleCreateUser handleForbidden

let handleAuthReadUsers : HttpHandler =
    Request.ifAuthenticatedWithScope readUsersPolicy authority handleReadUsers handleForbidden

let handleAuthUpdateUser : HttpHandler =
    Request.ifAuthenticatedWithScope updateUsersPolicy authority handleUpdateUser handleForbidden

let handleAuthDeleteUser : HttpHandler =
    Request.ifAuthenticatedWithScope deleteUsersPolicy authority handleDeleteUser handleForbidden

// ------------
// Register services
// ------------
let createTokenValidationParameters () =
    let tvp = new TokenValidationParameters()
    tvp.NameClaimType <- ClaimTypes.NameIdentifier
    tvp

let configureServices (services : IServiceCollection) =
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

// ------------
// Activate middleware
// ------------
let configureApp (endpoints : HttpEndpoint list) (app : IApplicationBuilder) =    
    app.UseAuthentication()
       .UseFalco(endpoints) |> ignore

// ------------
// Web host
// ------------
let configureWebhost (endpoints : HttpEndpoint list) (webhost : IWebHostBuilder) =
    webhost.ConfigureServices(configureServices)
           .Configure(configureApp endpoints)

[<EntryPoint>]
let main args =        
    try
        webHost args {
            configure configureWebhost

            endpoints [   
                get "/" handleIndex

                get "/users" handleAuthReadUsers

                post "/users" handleAuthCreateUser
                
                put "/users/{id:guid}" handleAuthUpdateUser

                delete "/users/{id:guid}" handleAuthDeleteUser
            ]
        }           
        0
    with 
    | ex -> 
        printfn "%s\n\n%s" ex.Message ex.StackTrace
        -1