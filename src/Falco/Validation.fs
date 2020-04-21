[<AutoOpen>]
module Falco.Validation

open System.Text.RegularExpressions

/// A perl-style infix operator to check match against Regular Expression
let (=~) input pattern =
    Regex.IsMatch(input, pattern, RegexOptions.Multiline)

/// A perl-style infix operator to check for non-match against Regular Expression
let (!=~) input pattern =
    not(input =~ pattern)

/// Attempt to validate model using the provided `validate` function
let tryValidateModel
    (validate : 'a -> Result<'a, string * 'a> ) 
    (error : string -> 'a -> HttpHandler) 
    (success : 'a -> HttpHandler) 
    (model : 'a ) : HttpHandler =    
    match validate model with
    | Ok _      -> success model
    | Error (err, _) -> error err model

