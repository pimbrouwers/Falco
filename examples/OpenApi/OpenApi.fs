namespace OpenApi

open Falco
open Falco.OpenApi
open Falco.Routing
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

type FortuneInput =
    { Name : string }

type Fortune =
    { Description : string }
    static member Create name age color = { Description = "placeholder fortune" }

type Greeting =
    { Message : string }

module Program =
    let endpoints =
        [
            mapGet "/fortune/{name?}"
                (fun route ->
                    let name = route?name.AsString("stranger")
                    let age = route?age.AsInt32Option()
                    let color = route?color.AsStringOption()
                    Fortune.Create name age color)
                Response.ofJson
                |> OpenApi.name "Fortune"
                |> OpenApi.description "A mystic fortune teller"
                |> OpenApi.acceptsType typeof<FortuneInput>
                |> OpenApi.returnType typeof<Fortune>

            mapGet "/hello/{name?}" (fun route -> { Message = route?name.AsString("world") }) Response.ofJson
                |> OpenApi.name "Greeting"
                |> OpenApi.description "A friendly greeter"
                |> OpenApi.acceptsType typeof<string>
                |> OpenApi.returnType typeof<Greeting>

            get "/" (Response.ofPlainText "Hello World!")
                |> OpenApi.name "HelloWorld"
                |> OpenApi.description "This is a test"
                |> OpenApi.returnType typeof<string>
        ]

    [<EntryPoint>]
    let main args =
        let bldr = WebApplication.CreateBuilder(args)

        bldr.Services
            .AddFalcoOpenApi()
            .AddSwaggerGen()
            |> ignore

        let wapp = bldr.Build()

        wapp.UseHttpsRedirection()
            .UseSwagger()
            .UseSwaggerUI()
        |> ignore

        wapp.UseFalco(endpoints)
        |> ignore

        wapp.Run()
        0
