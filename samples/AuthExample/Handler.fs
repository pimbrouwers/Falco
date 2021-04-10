module AuthExample.Handler

open Falco
open AuthExample.Domain
open AuthExample.Repository

// ------------
// Init Interactors 
// ------------
let storage = MemoryStorage()

let getAllUsersWithStorage = getAllUsers storage
let createUserWithStorage = createUser storage
let updateUserWithStorage = updateUser storage
let deleteUserWithStorage = deleteUser storage

// ------------
// General Purpose Handlers
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
    | Result.Ok result   -> Response.ofJson result
    | Result.Error error -> handleError error

// ------------
// Specific Handlers
// ------------
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
                | None    -> Result.Error "No user id provided")
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
                | None    -> Result.Error "No user id provided")
        (fun id ->
            deleteUserWithStorage id
            |> handleResult)
        (fun _ -> handleBadRequest)
