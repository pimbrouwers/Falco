module Falco.Multipart.Tests

open System.Collections.Generic
open System.IO
open System.Text
open System.Threading.Tasks
open Falco
open Falco.Multipart
open FSharp.Control.Tasks.V2.ContextInsensitive
open FsUnit.Xunit
open NSubstitute
open Xunit
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.WebUtilities
open Microsoft.Extensions.Primitives
open Microsoft.Net.Http.Headers
open System.Security.Claims

[<Fact>]
let ``MultipartReader.StreamFormAsync() with 3-part body`` () =
    let threePartBody =
            "--9051914041544843365972754266\r\n" +
            "Content-Disposition: form-data; name=\"text\"\r\n" +
            "\r\n" +
            "text default\r\n" +
            "--9051914041544843365972754266\r\n" +
            "Content-Disposition: form-data; name=\"file1\"; filename=\"a.txt\"\r\n" +
            "Content-Type: text/plain\r\n" +
            "\r\n" +
            "Content of a.txt.\r\n" +
            "\r\n" +
            "--9051914041544843365972754266\r\n" +
            "Content-Disposition: form-data; name=\"file2\"; filename=\"a.html\"\r\n" +
            "Content-Type: text/html\r\n" +
            "\r\n" +
            "<!DOCTYPE html><title>Content of a.html.</title>\r\n" +
            "\r\n" +
            "--9051914041544843365972754266--\r\n"

    use body = new MemoryStream(Encoding.UTF8.GetBytes(threePartBody))

    let rd = new MultipartReader("--9051914041544843365972754266", body)

    task {
        let! form = rd.StreamFormAsync()
        true |> should equal false
    }