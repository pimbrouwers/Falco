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
    Request.mapJson
        (fun (userDto : UserDto) -> 
            createUserWithStorage userDto
            |> handleResult)
        
let handleReadUsers : HttpHandler =
    Request.mapRoute
        (ignore)
        (fun _ -> 
            getAllUsersWithStorage ()
            |> handleResult)
    
let handleUpdateUser : HttpHandler =
    Request.mapRoute
        (fun routeCollection -> routeCollection.GetString "id" "")
        (fun id ->
            Request.mapJson
                (fun (userDto : UserDto) -> 
                    updateUserWithStorage id userDto
                    |> handleResult))                
            
let handleDeleteUser : HttpHandler =
    Request.mapRoute
        (fun routeCollection -> routeCollection.GetString "id" "")
        (fun id ->
            deleteUserWithStorage id
            |> handleResult)
        