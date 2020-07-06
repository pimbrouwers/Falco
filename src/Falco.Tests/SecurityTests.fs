module Falco.Tests.Security

open FsUnit.Xunit
open Xunit

module Crypto =
    open Falco.Security.Crypto

    [<Fact>]
    let ``bytesFromBase64 should return bytes for base64 string`` () =
        let expected = [|102uy; 97uy; 108uy; 99uy; 111uy|]
        let base64 = "ZmFsY28="
        base64
        |> bytesFromBase64
        |> should equal expected
        
    [<Fact>]
    let ``bytesToBase64 should return base64 string for bytes`` () =
        let expected = "ZmFsY28="
        let bytes = [|102uy; 97uy; 108uy; 99uy; 111uy|]
        bytes
        |> bytesToBase64
        |> should equal expected

    [<Theory>]
    [<InlineData(0, 10000)>]
    [<InlineData(100000, 150000)>]
    let ``randomInt should produce int between min & max`` 
        (min : int) 
        (max : int) =
        randomInt min max
        |> fun i -> 
            (i >= min && i <= max)
            |> should equal true

    [<Fact>]
    let ``createSalt should generate a random salt of specified length`` () =
        let salt = createSalt 16
        salt
        |> fun s -> s.Length |> should equal 24

    [<Fact>]
    let ``sha256 should produce 44 character string from inputs`` () =
        let iterations = randomInt 150000 200000
        let byteLen = 32
        let salt = createSalt 16
        let password = createSalt 16

        sha256 iterations byteLen salt password
        |> fun hash -> hash.Length = 44
