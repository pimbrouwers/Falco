namespace AuthExample

open Falco
open Microsoft.Extensions.DependencyInjection

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
