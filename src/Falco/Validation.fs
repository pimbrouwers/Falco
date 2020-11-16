module Falco.Validation

open System

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

    type EqualityValidator<'a when 'a : equality>() =                 
        member _.equals (equalTo : 'a) (message : string option) : Validator<'a> =
            let defaultMessage () = sprintf "Value must be equal to %A" equalTo
            fun (value) -> ValidationResult.create (value = equalTo) value (messageOrDefault message defaultMessage)

        member _.notEquals (notEqualTo : 'a) (message : string option) : Validator<'a> =
            let defaultMessage () = sprintf "Value must not equal %A" notEqualTo
            fun (value) -> ValidationResult.create (value <> notEqualTo) value (messageOrDefault message defaultMessage)    

    type ComparisonValidator<'a when 'a : comparison>() = 
        inherit EqualityValidator<'a>()

        member _.between (min : 'a) (max : 'a) (message : string option) : Validator<'a> =
            let defaultMessage () = sprintf "Value must be between %A and %A" min max
            fun (value) -> ValidationResult.create (value >= min && value <= max) value (messageOrDefault message defaultMessage)
                
        member _.greaterThan (min : 'a) (message : string option) : Validator<'a> =
            let defaultMessage () = sprintf "Value must be greater than or equal to %A" min
            fun (value) -> ValidationResult.create (value >= min) value (messageOrDefault message defaultMessage)

        member _.lessThan (max : 'a) (message : string option) : Validator<'a> =
            let defaultMessage () = sprintf "Value must be less than or equal to %A" min
            fun (value) -> ValidationResult.create (value <= max) value (messageOrDefault message defaultMessage)

    type StringValidator() =
        inherit EqualityValidator<string>() 

        member _.betweenLen (min : int) (max : int) (message : string option) (value : string) =
            let defaultMessage () = sprintf "Value must be between %i and %i characters" min max
            ValidationResult.create (value.Length >= min && value.Length <= max) value (messageOrDefault message defaultMessage)

        member _.empty (message : string option) (value : string) =
            let defaultMessage () = sprintf "Value must be empty"
            ValidationResult.create (String.IsNullOrWhiteSpace(value)) value (messageOrDefault message defaultMessage)

        member _.maxLen (max : int) (message : string option) (value : string) =
            let defaultMessage () = sprintf "Value must not execeed %i characters" max
            ValidationResult.create (value.Length <= max) value (messageOrDefault message defaultMessage)

        member _.minLen (min : int) (message : string option) (value : string) =
            let defaultMessage () = sprintf "Value must be at least %i characters" min
            ValidationResult.create (value.Length >= min) value (messageOrDefault message defaultMessage)

        member _.notEmpty (message : string option) (value : string) =
            let defaultMessage () = sprintf "Value must not be empty"
            ValidationResult.create (not(String.IsNullOrWhiteSpace(value))) value (messageOrDefault message defaultMessage)

        member _.pattern (pattern : string) (message : string option) (value : string) =
            let defaultMessage () = sprintf "Value must match pattern %s" pattern
            ValidationResult.create (Text.RegularExpressions.Regex.IsMatch(value, pattern)) value (messageOrDefault message defaultMessage)

    let optional (validator : string option -> 'a -> ValidationResult<'a>) (message : string option) (value : 'a option) : ValidationResult<'a option> =  
        match value with
        | Some v -> validator message v |> Result.map (fun v -> Some v)
        | None   -> Ok value

    let required (validator : string option -> 'a -> ValidationResult<'a>) (message : string option) (value : 'a option) : ValidationResult<'a> =  
        let defaultMessage () = sprintf "Value is required"
        match value with
        | Some v -> validator message v
        | None   -> Error [ messageOrDefault message defaultMessage ]
            
    let DateTime       = ComparisonValidator<DateTime>()
    let DateTimeOffset = ComparisonValidator<DateTimeOffset>()
    let Decimal        = ComparisonValidator<decimal>()
    let Float          = ComparisonValidator<float>()
    let Int            = ComparisonValidator<int>()
    let Int16          = ComparisonValidator<int16>()
    let Int64          = ComparisonValidator<int64>()
    let String         = StringValidator()
    let TimeSpan       = ComparisonValidator<TimeSpan>()