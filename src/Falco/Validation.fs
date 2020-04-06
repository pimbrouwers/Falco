[<AutoOpen>]
module Falco.Validation

open System.Text.RegularExpressions

/// A perl-style infix operator to check match against Regular Expression
let (=~) input pattern =
    Regex.IsMatch(input, pattern, RegexOptions.Multiline)

/// A perl-style infix operator to check for non-match against Regular Expression
let (!=~) input pattern =
    not(input =~ pattern)

/// Defines a type with a Validate function
type IModelValidator<'a> = abstract member Validate : unit -> Result<'a, string * 'a>    

/// Attempt to validate model
let tryValidate<'a when 'a :> IModelValidator<'a>> 
    (error : string -> 'a -> HttpHandler) 
    (success : 'a -> HttpHandler) 
    (model : 'a) : HttpHandler =
    match model.Validate() with
    | Ok _      -> success model
    | Error (err, _) -> error err model

