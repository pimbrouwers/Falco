open Falco
open Microsoft.AspNetCore.Builder 
open Microsoft.Extensions.DependencyInjection // <-- this import adds DI capabilities

type IGreeter = 
    abstract member Greet : name : string -> string

type FriendlyGreeter() = 
    interface IGreeter with 
        member _.Greet(name : string) =
            $"Hello {name} 😀"

let bldr = WebApplication.CreateBuilder() // <-- create a configurable web application builder

bldr.Services
    .AddSingleton<IGreeter, FriendlyGreeter>() // <-- register the greeter as singleton in the container
    |> ignore

let wapp = bldr.Build() // <-- manifest our WebApplication

wapp.UseFalco()
    .FalcoGet("/{name?}", fun ctx ->
        let greeter = ctx.Plug<IGreeter>() // <-- access our dependency from the container
        let route = Request.getRoute ctx
        let name = route.GetString("name", "world")
        let greeting = greeter.Greet(name) // <-- invoke our greeter.Greet(name) method
        Response.ofPlainText greeting ctx)
    .Run()
