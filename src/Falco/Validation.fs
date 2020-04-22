[<AutoOpen>]
module Falco.Validation

/// Attempt to validate model using the provided `validate` function
let tryValidateModel
    (validate : 'a -> Result<'a, string> ) 
    (error : string -> 'a -> HttpHandler) 
    (success : 'a -> HttpHandler) 
    (model : 'a ) : HttpHandler =    
    match validate model with
    | Ok _      -> success model
    | Error (err) -> error err model

