namespace Falco.IntegrationTests

open System.Net.Http
open Microsoft.AspNetCore.Mvc.Testing
open Xunit
open Falco.IntegrationTests.App

module FalcoOpenApiTestServer =
    let createFactory() =
        new WebApplicationFactory<Program>()

module Tests =
    let private factory = FalcoOpenApiTestServer.createFactory ()

    [<Fact>]
    let ``Receive plain-text response from: GET /hello``() =
        let client = factory.CreateClient ()
        let content = client.GetStringAsync("/").Result
        Assert.Equal("Hello World!", content)

    [<Fact>]
    let ``Receive text/html response from GET /html`` () =
        let client = factory.CreateClient ()
        let content = client.GetStringAsync("/html").Result
        Assert.Equal("""<!DOCTYPE html><html><head></head><body><h1>hello world</h1></body></html>""", content)


    [<Fact>]
    let ``Receive application/json response from GET /json`` () =
        let client = factory.CreateClient ()
        let content = client.GetStringAsync("/json").Result
        Assert.Equal("""{"Message":"hello world"}""", content)

    [<Fact>]
    let ``Receive mapped application/json response from: GET /hello/name?`` () =
        let client = factory.CreateClient ()
        let content = client.GetStringAsync("/hello").Result
        Assert.Equal("""{"Message":"Hello world!"}""", content)

        let content = client.GetStringAsync("/hello/John").Result
        Assert.Equal("""{"Message":"Hello John!"}""", content)

        let content = client.GetStringAsync("/hello/John?age=42").Result
        Assert.Equal("""{"Message":"Hello John, you are 42 years old!"}""", content)

    [<Fact>]
    let ``Receive mapped application/json response from: POST /hello/name?`` () =
        let client = factory.CreateClient ()
        use form = new FormUrlEncodedContent([])
        let response = client.PostAsync("/hello", form).Result
        let content = response.Content.ReadAsStringAsync().Result
        Assert.Equal("""{"Message":"Hello world!"}""", content)

        let response = client.PostAsync("/hello/John", form).Result
        let content = response.Content.ReadAsStringAsync().Result
        Assert.Equal("""{"Message":"Hello John!"}""", content)

        use form = new FormUrlEncodedContent(dict [ ("age", "42") ])
        let response = client.PostAsync("/hello/John", form).Result
        let content = response.Content.ReadAsStringAsync().Result
        Assert.Equal("""{"Message":"Hello John, you are 42 years old!"}""", content)

module Program = let [<EntryPoint>] main _ = 0
