namespace BasicRestApi

open System.Collections.Concurrent
open System.Collections.Generic
open Falco
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection

type Error =
    { Code : string
      Message : string }

type User =
    { Username : string
      FullName : string }

type IStore<'TKey, 'TItem> =
    abstract member GetAll : unit   -> 'TItem seq
    abstract member Add : 'TItem -> Result<unit, Error>
    abstract member Update : 'TItem -> Result<unit, Error>
    abstract member Remove : 'TKey -> Result<unit, Error>

type UserStore() =
    let value = ConcurrentDictionary<string, User>()

    interface IStore<string, User> with
        member _.GetAll() =
            value.Values

        member _.Add(user : User) =
            match value.ContainsKey(user.Username) with
            | false ->
                value.GetOrAdd(user.Username, user) |> ignore
                Ok ()
            | true ->
                Error { Code = "EXISTS"; Message = "This user already exists" }

        member _.Update(user : User) =
            match value.ContainsKey(user.Username) with
            | true ->
                value.AddOrUpdate(user.Username, user, fun _ _ -> user) |> ignore
                Ok ()
            | false ->
                Error { Code = "MISSING"; Message = "This user does not exist" }

        member _.Remove(username : string) =
            match value.ContainsKey(username) with
            | true ->
                value.Remove(username) |> ignore
                Ok ()
            | false ->
                Error { Code = "MISSING"; Message = "This user does not exist" }

module Route =
    let userIndex = "/"
    let userCreate = "/users"
    let userUpdate = "/users/{username}"
    let userDelete = "/users/{username}"

module ErrorPage =
    let notFound : HttpHandler =
        Response.withStatusCode 404 >>
        Response.ofJson { Code = "404"; Message = "Not Found" }

    let serverException : HttpHandler =
        Response.withStatusCode 500 >>
        Response.ofJson { Code = "500"; Message = "Server Error" }

module UserController =
    let private handleResult result =
        match result with
        | Ok result -> Response.ofJson result
        | Error error -> Response.withStatusCode 400 >> Response.ofJson error

    let index : HttpHandler = fun ctx ->
        let userStore = ctx.Plug<IStore<string, User>>()
        let allUsers = userStore.GetAll()
        Response.ofJson allUsers ctx

    let create : HttpHandler = fun ctx ->
        let userStore = ctx.Plug<IStore<string, User>>()
        Request.mapJson (fun userJson ->
            let userAddResult = userStore.Add(userJson)
            handleResult userAddResult) ctx

    let update : HttpHandler = fun ctx ->
        let userStore = ctx.Plug<IStore<string, User>>()
        Request.mapJson (fun userJson ->
            let userAddResult = userStore.Update(userJson)
            handleResult userAddResult) ctx

    let delete : HttpHandler = fun ctx ->
        let userStore = ctx.Plug<IStore<string, User>>()
        let route = Request.getRoute ctx
        let username = route?username.AsString()
        let userRemoveResult = userStore.Remove(username)
        handleResult userRemoveResult ctx

module Program =
    let endpoints =
        [ get Route.userIndex UserController.index
          post Route.userCreate UserController.create
          put Route.userUpdate UserController.update
          delete Route.userDelete UserController.delete ]

    [<EntryPoint>]
    let main args =
        let bldr = WebApplication.CreateBuilder(args)

        bldr.Services
            .AddSingleton<IStore<string, User>, UserStore>()
            |> ignore

        let wapp = bldr.Build()

        let isDevelopment = wapp.Environment.EnvironmentName = "Development"

        wapp.UseIf(isDevelopment, DeveloperExceptionPageExtensions.UseDeveloperExceptionPage)
            .UseIf(not(isDevelopment), FalcoExtensions.UseFalcoExceptionHandler ErrorPage.serverException)
            .UseFalco(endpoints)
            .Run()
        0