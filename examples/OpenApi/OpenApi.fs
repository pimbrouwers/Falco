namespace OpenApi

open Falco
open Falco.OpenApi
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

type FortuneInput =
    { Name : string }

type Fortune =
    { Description : string }
    static member Create age input =
        match age with
        | Some age when age > 0 ->
            { Description = $"{input.Name}, you will experience great success when you are {age + 3}." }
        | _ ->
            { Description = $"{input.Name}, your future is unclear." }

type Greeting =
    { Message : string }

module Program =
    open Falco.Routing

    let endpoints =
        [
            mapPost "/fortune"
                (fun r -> r?age.AsIntOption())
                (fun ageOpt ->
                    Request.mapJson<FortuneInput> (Option.defaultValue { Name = "Joe" }
                    >> Fortune.Create ageOpt
                    >> Response.ofJson))
                |> OpenApi.name "Fortune"
                |> OpenApi.summary "A mystic fortune teller"
                |> OpenApi.description "Get a glimpse into your future, if you dare."
                |> OpenApi.query [
                    { Type = typeof<int>; Name = "Age"; Required = false } ]
                |> OpenApi.acceptsType typeof<FortuneInput>
                |> OpenApi.returnType typeof<Fortune>

            mapGet "/hello/{name?}"
                (fun route ->
                    { Message = route?name.AsString("world") })
                Response.ofJson
                |> OpenApi.name "Greeting"
                |> OpenApi.description "A friendly greeter"
                |> OpenApi.route [
                    { Type = typeof<string>; Name = "Name"; Required = false } ]
                |> OpenApi.query [
                    { Type = typeof<int>; Name = "Age"; Required = false } ]
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
