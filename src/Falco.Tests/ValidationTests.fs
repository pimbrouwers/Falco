module Falco.Tests.Validation

open Xunit
open Falco.Validation
open FsUnit.Xunit

[<Theory>]
[<InlineData(false)>]
[<InlineData(true)>]
let ``ValidationResult.create produces result based on condition`` (isValid : bool) =
    let errorMessage = "fake error message"
    let validationResult = ValidationResult.create isValid () errorMessage
    
    if isValid then validationResult |> should equal (Ok ())
    else validationResult |> should equal (Error [ errorMessage ])