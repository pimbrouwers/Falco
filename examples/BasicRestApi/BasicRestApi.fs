namespace BasicRestApi

open System.Data
open Donald // <-- external package that makes using databases simpler
open Falco
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Data.Sqlite // <-- a very useful Microsoft package

type Error =
    { Code : string
      Message : string }

type User =
    { Username : string
      FullName : string }

type IDbConnectionFactory =
    abstract member Create : unit -> IDbConnection

type IStore<'TKey, 'TItem> =
    abstract member GetAll : unit   -> 'TItem seq
    abstract member Add : 'TItem -> Result<unit, Error>
    abstract member Get : 'TKey -> 'TItem option
    abstract member Remove : 'TKey -> Result<unit, Error>

type UserStore(dbConnection : IDbConnectionFactory) =
    let userOfDataReader (rd : IDataReader) =
        { Username = rd.ReadString "username"
          FullName = rd.ReadString "full_name" }

    interface IStore<string, User> with
        member _.GetAll() =
            use conn = dbConnection.Create()
            conn
            |> Db.newCommand "SELECT username, full_name FROM user"
            |> Db.query userOfDataReader
            |> Seq.ofList

        member _.Add(user : User) =
            use conn = dbConnection.Create()
            try
                conn
                |> Db.newCommand "
                    INSERT INTO user (username, full_name)
                    SELECT    @username
                            , @full_name
                    WHERE     @username NOT IN (
                                SELECT username FROM user)"
                |> Db.setParams [
                    "username", SqlType.String user.Username
                    "full_name", SqlType.String user.FullName ]
                |> Db.exec
                |> Ok
            with
            | :? DbExecutionException ->
                Error { Code = "FAILED"; Message = "Could not add user" }

        member _.Get(username : string) =
            use conn = dbConnection.Create()
            conn
            |> Db.newCommand "
                SELECT    username
                        , full_name
                FROM      user
                WHERE     username = @username"
            |> Db.setParams [ "username", SqlType.String username ]
            |> Db.querySingle userOfDataReader

        member _.Remove(username : string) =
            use conn = dbConnection.Create()
            try
                conn
                |> Db.newCommand "DELETE FROM user WHERE username = @username"
                |> Db.setParams [ "username", SqlType.String username ]
                |> Db.exec
                |> Ok
            with
            | :? DbExecutionException ->
                Error { Code = "FAILED"; Message = "Could not add user" }

module Route =
    let userIndex = "/"
    let userCreate = "/users"
    let userGet = "/users/{username}"
    let userDelete = "/users/{username}"

module ErrorPage =
    let badRequest error : HttpHandler =
        Response.withStatusCode 400
        >> Response.ofJson error

    let notFound : HttpHandler =
        Response.withStatusCode 404 >>
        Response.ofJson { Code = "404"; Message = "Not Found" }

    let serverException : HttpHandler =
        Response.withStatusCode 500 >>
        Response.ofJson { Code = "500"; Message = "Server Error" }

module UserEndpoint =
    let index : HttpHandler = fun ctx ->
        let userStore = ctx.Plug<IStore<string, User>>()
        let allUsers = userStore.GetAll()
        Response.ofJson allUsers ctx

    let create : HttpHandler = fun ctx -> task {
        let userStore = ctx.Plug<IStore<string, User>>()
        let! userJson = Request.getJson<User> ctx
        let userAddResponse =
            match userStore.Add(userJson) with
            | Ok result -> Response.ofJson result ctx
            | Error error -> ErrorPage.badRequest error ctx
        return! userAddResponse }

    let get : HttpHandler = fun ctx ->
        let userStore = ctx.Plug<IStore<string, User>>()
        let route = Request.getRoute ctx
        let username = route?username.AsString()
        match userStore.Get(username) with
        | Some user -> Response.ofJson user ctx
        | None -> ErrorPage.notFound ctx

    let delete : HttpHandler = fun ctx ->
        let userStore = ctx.Plug<IStore<string, User>>()
        let route = Request.getRoute ctx
        let username = route?username.AsString()
        match userStore.Remove(username) with
        | Ok result -> Response.ofJson result ctx
        | Error error -> ErrorPage.badRequest error ctx

module Program =
    let endpoints =
        [ get Route.userIndex UserEndpoint.index
          post Route.userCreate UserEndpoint.create
          get Route.userGet UserEndpoint.get
          delete Route.userDelete UserEndpoint.delete ]

    [<EntryPoint>]
    let main args =
        let bldr = WebApplication.CreateBuilder(args)

        let initializeDatabase (dbConnection : IDbConnectionFactory) =
            use conn = dbConnection.Create()
            conn
            |> Db.newCommand "CREATE TABLE IF NOT EXISTS user (username, full_name)"
            |> Db.exec

        let dbConnectionFactory =
            { new IDbConnectionFactory with
                member _.Create() = new SqliteConnection("Data Source=:memory:") }

        initializeDatabase dbConnectionFactory

        bldr.Services
            .AddSingleton<IDbConnectionFactory>(dbConnectionFactory)
            .AddSingleton<IStore<string, User>, UserStore>()
            |> ignore

        let wapp = bldr.Build()

        let isDevelopment = wapp.Environment.EnvironmentName = "Development"

        wapp.UseIf(isDevelopment, DeveloperExceptionPageExtensions.UseDeveloperExceptionPage)
            .UseIf(not(isDevelopment), FalcoExtensions.UseFalcoExceptionHandler ErrorPage.serverException)
            .UseFalco(endpoints)
            .Run()
        0