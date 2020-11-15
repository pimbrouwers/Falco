module Falco.Validation

type ValidationErrors = string list

type ValidationResult<'a> = Result<'a, ValidationErrors>

type Validator<'a> = 'a -> ValidationResult<'a>

module ValidationResult = 
    let create condition value error : ValidationResult<'a> =
        if condition then Ok value
        else [error] |> Error

    let apply (resultFn : ValidationResult<'a -> 'b>) (result : ValidationResult<'a>) =
        match resultFn, result with
        | Ok fn, Ok x        -> fn x |> Ok
        | Error e, Ok _      -> Error e
        | Ok _, Error e      -> Error e
        | Error e1, Error e2 -> List.concat [e1;e2] |> Error

    let map (fn : 'a -> 'b) (result : ValidationResult<'a>) =
        match result with 
        | Ok x    -> fn x |> Ok
        | Error e -> Error e

    let mapError (errorFn : ValidationErrors -> 'b) (result : ValidationResult<'a>) =
        match result with
        | Ok x -> Ok x
        | Error e -> e |> errorFn |> Error

let (<*>) = ValidationResult.apply
let (<!>) = ValidationResult.map

[<RequireQualifiedAccess>]
module Validators =
    let private messageOrDefault (message : string option) (defaultMessage : unit -> string) =
        message |> Option.defaultValue (defaultMessage ())

    let optional (validator : string option -> 'a -> ValidationResult<'a>) (message : string option) (value : 'a option) : ValidationResult<'a option> =  
        match value with
        | Some v -> validator message v |> Result.map (fun v -> Some v)
        | None   -> Ok value

    let required (validator : string option -> 'a -> ValidationResult<'a>) (message : string option) (value : 'a option) : ValidationResult<'a> =  
        let defaultMessage () = sprintf "Value is required"
        match value with
        | Some v -> validator message v
        | None   -> Error [ messageOrDefault message defaultMessage ]
    
    module private Numeric = 
        let between (min : 'a) (max : 'a) (message : string option) : Validator<'a> =
            let defaultMessage () = sprintf "Value must be between %A and %A" min max
            fun (value) -> ValidationResult.create (value >= min && value <= max) value (messageOrDefault message defaultMessage)

        let equals (equalTo : 'a) (message : string option) : Validator<'a> =
            let defaultMessage () = sprintf "Value must be equal to %A" equalTo
            fun (value) -> ValidationResult.create (value = equalTo) value (messageOrDefault message defaultMessage)

        let greaterThan (min : 'a) (message : string option) : Validator<'a> =
            let defaultMessage () = sprintf "Value must be greater than or equal to %A" min
            fun (value) -> ValidationResult.create (value >= min) value (messageOrDefault message defaultMessage)

        let lessThan (max : 'a) (message : string option) : Validator<'a> =
            let defaultMessage () = sprintf "Value must be less than or equal to %A" min
            fun (value) -> ValidationResult.create (value <= max) value (messageOrDefault message defaultMessage)

        let notEquals (notEqualTo : 'a) (message : string option) : Validator<'a> =
            let defaultMessage () = sprintf "Value must not equal %A" notEqualTo
            fun (value) -> ValidationResult.create (value <> notEqualTo) value (messageOrDefault message defaultMessage)

    //module DateTime = 
    //    open System

    module Decimal = 
        let between (min : decimal) (max : decimal) (message : string option) (value : decimal) = Numeric.between min max message value
        let equals (equalTo : decimal) (message : string option) (value : decimal)              = Numeric.equals equalTo message value
        let greaterThan (min : decimal) (message : string option) (value : decimal)             = Numeric.greaterThan min message value
        let lessThan (max : decimal) (message : string option) (value : decimal)                = Numeric.lessThan max message value
        let notEquals (notEqualTo : decimal) (message : string option) (value : decimal)        = Numeric.notEquals notEqualTo message value
    
    module Float = 
        let between (min : float) (max : float) (message : string option) (value : float) = Numeric.between min max message value
        let equals (equalTo : float) (message : string option) (value : float)            = Numeric.equals equalTo message value
        let greaterThan (min : float) (message : string option) (value : float)           = Numeric.greaterThan min message value
        let lessThan (max : float) (message : string option) (value : float)              = Numeric.lessThan max message value
        let notEquals (notEqualTo : float) (message : string option) (value : float)      = Numeric.notEquals notEqualTo message value

    module Int = 
        let between (min : int) (max : int) (message : string option) (value : int) = Numeric.between min max message value
        let equals (equalTo : int) (message : string option) (value : int)          = Numeric.equals equalTo message value
        let greaterThan (min : int) (message : string option) (value : int)         = Numeric.greaterThan min message value
        let lessThan (max : int) (message : string option) (value : int)            = Numeric.lessThan max message value
        let notEquals (notEqualTo : int) (message : string option) (value : int)    = Numeric.notEquals notEqualTo message value

    module Int16 = 
        let between (min : int16) (max : int16) (message : string option) (value : int16) = Numeric.between min max message value
        let equals (equalTo : int16) (message : string option) (value : int16)            = Numeric.equals equalTo message value
        let greaterThan (min : int16) (message : string option) (value : int16)           = Numeric.greaterThan min message value
        let lessThan (max : int16) (message : string option) (value : int16)              = Numeric.lessThan max message value
        let notEquals (notEqualTo : int16) (message : string option) (value : int16)      = Numeric.notEquals notEqualTo message value

    module Int64 = 
        let between (min : int64) (max : int64) (message : string option) (value : int64) = Numeric.between min max message value
        let equals (equalTo : int64) (message : string option) (value : int64)            = Numeric.equals equalTo message value
        let greaterThan (min : int64) (message : string option) (value : int64)           = Numeric.greaterThan min message value
        let lessThan (max : int64) (message : string option) (value : int64)              = Numeric.lessThan max message value
        let notEquals (notEqualTo : int64) (message : string option) (value : int64)      = Numeric.notEquals notEqualTo message value

    //module String =
    //    let minLength (min : int) (fieldName : string) (value : string) =
    //        ValidationResult.create (value.Length >= min) value (Messages.gteSuffix (Some "characters") fieldName min)
