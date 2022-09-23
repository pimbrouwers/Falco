module Falco.Tests.Multipart

open System.IO
open System.Text
open Falco
open Falco.Multipart
open FSharp.Control.Tasks.V2.ContextInsensitive
open FsUnit.Xunit
open Xunit
open Microsoft.AspNetCore.WebUtilities

[<Fact>]
let ``MultipartReader.StreamSectionsAsync()`` () =
    let onePartBody =
        "--9051914041544843365972754266\r\n" +
        "Content-Disposition: form-data; name=\"name\"\r\n" +
        "\r\n" +
        "falco\r\n" +
        "--9051914041544843365972754266--\r\n";

    use body = new MemoryStream(Encoding.UTF8.GetBytes(onePartBody))

    let rd = new MultipartReader("9051914041544843365972754266", body)

    task {
        let! form = rd.StreamSectionsAsync()
        form.Files.Count |> should equal 0

        let formReader = FormCollectionReader(form, Some form.Files)
        let formValue = formReader.GetString "name"
        formValue |> should equal "falco"
    }

[<Fact>]
let ``MultipartReader.StreamSectionsAsync() with 3-part body`` () =
    let threePartBody =
            "--9051914041544843365972754266\r\n" +
            "Content-Disposition: form-data; name=\"name\"\r\n" +
            "\r\n" +
            "falco\r\n" +
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

    let rd = new MultipartReader("9051914041544843365972754266", body)

    task {
        let! form = rd.StreamSectionsAsync()
        form.Files.Count |> should equal 2

        // can we access the files?
        use ms = new MemoryStream()
        use st1 = form.Files.[0].OpenReadStream()
        st1.CopyTo(ms)

        ms.SetLength(0)
        use st2 = form.Files.[1].OpenReadStream()
        st1.CopyTo(ms)

        let formReader = FormCollectionReader(form, Some form.Files)
        let formValue = formReader.GetString "name"
        formValue |> should equal "falco"
    }