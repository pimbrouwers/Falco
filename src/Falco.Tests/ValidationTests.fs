module Falco.Tests.Validation

open Xunit
open Falco.Validation
open Falco.Validation.Operators
open FsUnit.Xunit

type FakeValidationRecord = { Name : string; Age : int }

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
    let result : ValidationResult<FakeValidationRecord> = 
        fun name age -> { Name = name; Age = age }
        <!> Validators.String.minLen 3 None "Pim"
        <*> Validators.Int.greaterThan 0 None 1

    result 
    |> Result.bind (fun r -> Ok(r |> should equal { Name = "Pim"; Age = 1 }))