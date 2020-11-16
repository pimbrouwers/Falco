module Falco.Tests.Validation

open Xunit
open Falco.Validation
open FsUnit.Xunit

[<Fact>]
let ``ValidationResult.create produces Ok result`` () =    
    ValidationResult.create true () ""
    |> Result.bind (fun result -> Ok (result |> should equal ()))

[<Fact>]
let ``ValidationResult.create produces Error result`` () =
    let errorMessage = "fake error message"
    
    ValidationResult.create false () errorMessage
    |> Result.mapError (fun errors -> errors |> should equal [ errorMessage ])

[<Fact>]
let ``Validation`` () =
    let result : ValidationResult<FakeRecord> = 
        fun name -> { Name = name }
        <!> Validators.String.minLen 3 None "Pim"

    result 
    |> Result.bind (fun r -> Ok(r |> should equal { Name = "Pim" }))