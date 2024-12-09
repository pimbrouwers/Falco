namespace BasicRestApi

open System
open System.Data
open System.Threading.Tasks
open Donald // <-- external package that makes using databases simpler
open Falco
open Falco.Routing
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.Data.Sqlite // <-- a very useful Microsoft package

type Error =
    { Code : string
      Message : string
      ExceptionDetail : string }

type User =
    { Username : string
      FullName : string }

type IDbConnectionFactory =
    abstract member Create : unit -> IDbConnection

type IStore<'TKey, 'TItem> =
    abstract member List : unit   -> Task<'TItem list>
    abstract member Create : 'TItem -> Task<Result<unit, Error>>
    abstract member Read : 'TKey -> Task<'TItem option>
    abstract member Delete : 'TKey -> Task<Result<unit, Error>>

type UserStore(dbConnection : IDbConnectionFactory) =
    let userOfDataReader (rd : IDataReader) =
        { Username = rd.ReadString "username"
          FullName = rd.ReadString "full_name" }

    interface IStore<string, User> with
        member _.List() =
            use conn = dbConnection.Create()
            conn
            |> Db.newCommand "SELECT username, full_name FROM user"
            |> Db.Async.query userOfDataReader

        member _.Create(user : User) =
            use conn = dbConnection.Create()
            use cmd =
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
            task {
                try
                    do! cmd |> Db.Async.exec
                    return Ok ()
                with
                | :? DbExecutionException as ex ->
                    return Error {
                        Code = "FAILED"
                        Message = "Could not add user"
                        ExceptionDetail = ex.ToString() }
            }

        member _.Read(username : string) =
            use conn = dbConnection.Create()
            conn
            |> Db.newCommand "
                SELECT    username
                        , full_name
                FROM      user
                WHERE     username = @username"
            |> Db.setParams [ "username", SqlType.String username ]
            |> Db.Async.querySingle userOfDataReader

        member _.Delete(username : string) =
            use conn = dbConnection.Create()
            use cmd =
                conn
                |> Db.newCommand "DELETE FROM user WHERE username = @username"
                |> Db.setParams [ "username", SqlType.String username ]

            task {
                try
                    do! cmd |> Db.Async.exec
                    return Ok ()
                with
                | :? DbExecutionException as ex ->
                    return Error {
                        Code = "FAILED"
                        Message = "Could not add user"
                        ExceptionDetail = ex.ToString() }
            }

module Route =
    let userIndex = "/"
    let userAdd = "/users"
    let userView = "/users/{username}"
    let userRemove = "/users/{username}"

module ErrorResponse =
    type ErrorDto =
        { Code : string
          Message : string }

    let badRequest (error : Error) : HttpHandler = fun ctx ->
        let log = ctx.Plug<ILogger<Error>>()
        log.LogError(error.ExceptionDetail, error)
        ctx
        |> Response.withStatusCode 400
        |> Response.ofJson { Code = error.Code; Message = error.Message }

    let notFound : HttpHandler =
        Response.withStatusCode 404 >>
        Response.ofJson { Code = "404"; Message = "Not Found" }

    let serverException : HttpHandler =
        Response.withStatusCode 500 >>
        Response.ofJson { Code = "500"; Message = "Server Error" }

module UserEndpoint =
    let index : HttpHandler = fun ctx ->
        let userStore = ctx.Plug<IStore<string, User>>()
        task {
            let! allUsers = userStore.List()
            return! Response.ofJson allUsers ctx
        }

    let add : HttpHandler = fun ctx ->
        let userStore = ctx.Plug<IStore<string, User>>()
        task {
            let! userJson = Request.getJson<User> ctx
            let! userAddResult = userStore.Create(userJson)

            return!
                match userAddResult with
                | Ok result -> Response.ofJson result ctx
                | Error error -> ErrorResponse.badRequest error ctx
        }

    let view : HttpHandler = fun ctx ->
        let userStore = ctx.Plug<IStore<string, User>>()
        let route = Request.getRoute ctx
        let username = route?username.AsString()
        task {
            let! userReadResult = userStore.Read(username)
            return!
                match userReadResult with
                | Some user -> Response.ofJson user ctx
                | None -> ErrorResponse.notFound ctx
        }

    let remove : HttpHandler = fun ctx ->
        let userStore = ctx.Plug<IStore<string, User>>()
        let route = Request.getRoute ctx
        let username = route?username.AsString()
        task {
            let! userDeleteResult = userStore.Delete(username)
            return!
                match userDeleteResult with
                | Ok result -> Response.ofJson result ctx
                | Error error -> ErrorResponse.badRequest error ctx
        }

module Program =
    let endpoints =
        [ get Route.userIndex UserEndpoint.index
          post Route.userAdd UserEndpoint.add
          get Route.userView UserEndpoint.view
          delete Route.userRemove UserEndpoint.remove ]

    let initializeDatabase (dbConnection : IDbConnectionFactory) =
        use conn = dbConnection.Create()
        conn
        |> Db.newCommand "CREATE TABLE IF NOT EXISTS user (username, full_name)"
        |> Db.exec

    [<EntryPoint>]
    let main args =
        let bldr = WebApplication.CreateBuilder(args)
        let isDevelopment = bldr.Environment.EnvironmentName = "Development"
        let conf = bldr.Configuration

        let dbConnectionFactory =
            { new IDbConnectionFactory with
                member _.Create() = new SqliteConnection(conf.GetConnectionString("BasicRestApiEvolved")) }

        initializeDatabase dbConnectionFactory

        bldr.AddLogging(fun logBuilder -> logBuilder.AddConsole())
            .Services
            .AddSingleton<IDbConnectionFactory>(dbConnectionFactory)
            .AddSingleton<IStore<string, User>, UserStore>()
            |> ignore

        let wapp = bldr.Build()

        wapp.UseIf(isDevelopment, DeveloperExceptionPageExtensions.UseDeveloperExceptionPage)
            .UseIf(not(isDevelopment), FalcoExtensions.UseFalcoExceptionHandler ErrorResponse.serverException)
            .UseRouting()
            .UseFalco(endpoints)
            .Run()
        0
