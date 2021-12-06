module Falco.Tests.Crypto

open FsUnit.Xunit
open Xunit
open Falco.Security

[<Fact>]
let ``bytesFromBase64 should return bytes for base64 string`` () =
    let expected = [|102uy; 97uy; 108uy; 99uy; 111uy|]
    let base64 = "ZmFsY28="
    base64
    |> Crypto.bytesFromBase64
    |> should equal expected
        
[<Fact>]
let ``bytesToBase64 should return base64 string for bytes`` () =
    let expected = "ZmFsY28="
    let bytes = [|102uy; 97uy; 108uy; 99uy; 111uy|]
    bytes
    |> Crypto.bytesToBase64
    |> should equal expected

[<Theory>]
[<InlineData(0, 10000)>]
[<InlineData(100000, 150000)>]
let ``randomInt should produce int between min & max`` 
    (min : int) 
    (max : int) =
    Crypto.randomInt min max
    |> fun i -> 
        (i >= min && i <= max)
        |> should equal true

[<Fact>]
let ``createSalt should generate a random salt of specified length`` () =
    let salt = Crypto.createSalt 16
    salt
    |> fun s -> s.Length |> should equal 24


let salt = "8BSBv62T/qi2Yf10QBN4Zw=="
let iterations = 150000
let password = "falco"

[<Fact>]
let ``sha256 should produce expected 44 character string from inputs`` () =
    let expected = "Id62vc62JiHjAXT4m8Ie3nF3fwDZDp256Ug+6crAZyI="
    let byteLen = 32
    let hash = Crypto.sha256 iterations byteLen salt password

    hash.Length = 44 |> should equal true
    hash |> should equal expected

[<Fact>]
let ``sha512 should produce expected 44 character string from inputs`` () =
    let expected = "H+g5yVaqtF49c6ZBT7FL/2UJymeWLjyqOMjG8S/bowI="
    let byteLen = 32
    let hash = Crypto.sha512 iterations byteLen salt password

    hash.Length = 44 |> should equal true
    hash |> should equal expected
