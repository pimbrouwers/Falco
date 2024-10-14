namespace OpenApi

open Falco
open Falco.OpenApi
open Falco.Routing
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

module Program =
    let endpoints =
        [
            get "/" (Response.ofPlainText "Hello World!")
            |> OpenApi.name "TestName"
            |> OpenApi.description "This is a test"
            |> OpenApi.returnType typeof<string>
            // |> OpenApi.returns { Return = typeof<string>; ContentTypes = [ "text/plain" ]; Status = 200 }
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
