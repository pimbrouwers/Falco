module Falco.Tests.Validation

open Xunit
open Falco.Validation
open Falco.Validation.Operators
open FsUnit.Xunit

type FakeValidationRecord = { Name : string; Age : int }

[<Fact>]
let ``ValidationError.merges produces Map<string, string list> from two source`` () =
    let expected = [ "fakeField1", [ "fake error message 1" ]; "fakeField2", [ "fake error message 2" ] ] |> Map.ofList
    let error = ValidationErrors.create "fakeField1" [ "fake error message 1" ]
    let error2 = ValidationErrors.create "fakeField2" [ "fake error message 2" ]

    ValidationErrors.merge error error2
    |> should equal expected

[<Fact>]
let ``ValidationError.merges produces Map<string, string list> from two sources with same key`` () =
    let expected = [ "fakeField1", ["fake error message 1"; "fake error message 2" ] ] |> Map.ofList
    let error = ValidationErrors.create "fakeField1" [ "fake error message 1" ]
    let error2 = ValidationErrors.create "fakeField1" [ "fake error message 2" ]

    ValidationErrors.merge error error2
    |> should equal expected

[<Fact>]
let ``ValidationResult.create produces Ok result`` () =    
    ValidationResult.create true () ValidationErrors.empty
    |> Result.bind (fun result -> Ok (result |> should equal ()))

[<Fact>]
let ``ValidationResult.create produces Error result`` () =
    let errorMessage = "fake error message"
    let error = ValidationErrors.create "fakeField" [ errorMessage ]
    ValidationResult.create false () error
    |> Result.mapError (fun errors -> errors |> should equal error)

[<Fact>]
let ``Validation of record succeeds`` () =        
    let result : ValidationResult<FakeValidationRecord> = 
        fun name age -> { 
            Name = name
            Age = age
        }
        <!> Validators.String.minLen 3 "Name" None "John"
        <*> Validators.Int.greaterThan 0 "Age" None 1
    
    result 
    |> Result.bind (fun r -> Ok(r |> should equal { Name = "John"; Age = 1 }))