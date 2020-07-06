module Falco.Tests.StringUtils

open Xunit
open Falco.StringUtils
open FsUnit.Xunit

[<Fact>]    
let ``strJoin should combine strings`` () =
    [|"the";"man";"jumped";"high"|]
    |> strJoin " "
    |> should equal "the man jumped high"
            
[<Theory>]
[<InlineData("")>]
[<InlineData(null)>]
let ``strEmpty should be true`` str =
    str
    |> strEmpty
    |> should equal true

[<Fact>]
let ``strEmpty should be false`` () =
    "falco"
    |> strEmpty
    |> should equal false

[<Theory>]
[<InlineData("")>]
[<InlineData(null)>]
let ``strNotEmpty should be false`` str =
    str
    |> strNotEmpty
    |> should equal false

[<Fact>]
let ``strNotEmpty should be true`` () =
    "falco"
    |> strNotEmpty
    |> should equal true

[<Theory>]
[<InlineData("falco", "falco")>]
[<InlineData("falco", "FaLco")>]
let ``strEquals should be true`` str1 str2 =
    strEquals str1 str2
    |> should equal true

[<Fact>]
let ``strEquals should be false`` () =
    strEquals "falco" "aclaf"
    |> should equal false