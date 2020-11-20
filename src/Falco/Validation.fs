module Falco.Validation

open System
open Falco.StringUtils

type ValidationErrors = Map<string, string list>
type ValidationResult<'a> = Result<'a, ValidationErrors>
type Validator<'a> = string -> string option -> 'a -> ValidationResult<'a>

module ValidationErrors =
    let empty : ValidationErrors = Map.empty<string, string list>

    let create fieldName errors : ValidationErrors =   
        [ fieldName, errors ] |> Map.ofList

    let merge (e1 : ValidationErrors) (e2 : ValidationErrors) = 
        Map.fold 
            (fun acc k v -> 
                match Map.tryFind k acc with
                | Some v' -> Map.add k (v' @ v) acc
                | None    -> Map.add k v acc)
            e1
            e2

module ValidationResult = 
    /// Create a ValidationResult<'a> based on condition, yield
    /// error message if condition evaluates false
    let create condition value error : ValidationResult<'a> =
        if condition then Ok value
        else error |> Error

    /// Unpack ValidationResult and feed into validation function
    let apply (resultFn : ValidationResult<'a -> 'b>) (result : ValidationResult<'a>) : ValidationResult<'b> =
        match resultFn, result with
        | Ok fn, Ok x        -> fn x |> Ok
        | Error e, Ok _      -> Error e
        | Ok _, Error e      -> Error e
        | Error e1, Error e2 -> Error (ValidationErrors.merge e1 e2)  

    /// Unpack ValidationResult, evaluate function if Ok or return if Error
    let map (fn : 'a -> 'b) (result : ValidationResult<'a>) : ValidationResult<'b> =
        match result with 
        | Ok x    -> fn x |> Ok
        | Error e -> Error e

/// Custom operators for ValidationResult
module Operators =
    let (<*>) = ValidationResult.apply
    let (<!>) = ValidationResult.map

[<RequireQualifiedAccess>]
module Validators =
    let private messageOrDefault (fieldName : string) (message : string option) (defaultMessage : unit -> string) =        
        let message' = message |> Option.defaultValue (defaultMessage ())        
        ValidationErrors.create fieldName [message']

    type EqualityValidator<'a when 'a : equality>() =                 
        member _.equals (equalTo : 'a) : Validator<'a> =
            fun (fieldName : string) (message : string option) (value : 'a) -> 
                let defaultMessage () = sprintf "Value must be equal to %A" equalTo
                ValidationResult.create (value = equalTo) value (messageOrDefault fieldName message defaultMessage)

        member _.notEquals (notEqualTo : 'a) : Validator<'a> =
            fun (fieldName : string) (message : string option) (value : 'a) -> 
                let defaultMessage () = sprintf "Value must not equal %A" notEqualTo
                ValidationResult.create (value <> notEqualTo) value (messageOrDefault fieldName message defaultMessage)    

    type ComparisonValidator<'a when 'a : comparison>() = 
        inherit EqualityValidator<'a>()

        member _.between (min : 'a) (max : 'a) : Validator<'a> =
            fun (fieldName : string) (message : string option) (value : 'a) -> 
                let defaultMessage () = sprintf "Value must be between %A and %A" min max
                ValidationResult.create (value >= min && value <= max) value (messageOrDefault fieldName message defaultMessage)
                
        member _.greaterThan (min : 'a) : Validator<'a> =
            fun (fieldName : string) (message : string option) (value : 'a) -> 
                let defaultMessage () = sprintf "Value must be greater than or equal to %A" min
                ValidationResult.create (value >= min) value (messageOrDefault fieldName message defaultMessage)

        member _.lessThan (max : 'a) : Validator<'a> =
            fun (fieldName : string) (message : string option) (value : 'a) -> 
                let defaultMessage () = sprintf "Value must be less than or equal to %A" min
                ValidationResult.create (value <= max) value (messageOrDefault fieldName message defaultMessage)

    type StringValidator() =
        inherit EqualityValidator<string>() 

        member _.betweenLen (min : int) (max : int) (fieldName : string) (message : string option) (value : string) =
            let defaultMessage () = sprintf "Value must be between %i and %i characters" min max
            ValidationResult.create (value.Length >= min && value.Length <= max) value (messageOrDefault fieldName message defaultMessage)

        member _.empty (fieldName : string) (message : string option) (value : string) =
            let defaultMessage () = sprintf "Value must be empty"
            ValidationResult.create (strEmpty value) value (messageOrDefault fieldName message defaultMessage)

        member _.maxLen (max : int) (fieldName : string) (message : string option) (value : string) =
            let defaultMessage () = sprintf "Value must not execeed %i characters" max
            ValidationResult.create (value.Length <= max) value (messageOrDefault fieldName message defaultMessage)

        member _.minLen (min : int) (fieldName : string) (message : string option) (value : string) =
            let defaultMessage () = sprintf "Value must be at least %i characters" min
            ValidationResult.create (value.Length >= min) value (messageOrDefault fieldName message defaultMessage)

        member _.notEmpty (fieldName : string) (message : string option) (value : string) =
            let defaultMessage () = sprintf "Value must not be empty"
            ValidationResult.create (strNotEmpty value) value (messageOrDefault fieldName message defaultMessage)

        member _.pattern (pattern : string) (fieldName : string) (message : string option) (value : string) =
            let defaultMessage () = sprintf "Value must match pattern %s" pattern
            ValidationResult.create (Text.RegularExpressions.Regex.IsMatch(value, pattern)) value (messageOrDefault fieldName message defaultMessage)

    let optional (validator : string option -> 'a -> ValidationResult<'a>) (fieldName : string) (message : string option) (value : 'a option) : ValidationResult<'a option> =  
        match value with
        | Some v -> validator message v |> Result.map (fun v -> Some v)
        | None   -> Ok value

    let required (validator : string option -> 'a -> ValidationResult<'a>) (fieldName : string) (message : string option) (value : 'a option) : ValidationResult<'a> =  
        let defaultMessage () = sprintf "Value is required"
        match value with
        | Some v -> validator message v
        | None   -> Error (messageOrDefault fieldName message defaultMessage)
            
    let DateTime       = ComparisonValidator<DateTime>()
    let DateTimeOffset = ComparisonValidator<DateTimeOffset>()
    let Decimal        = ComparisonValidator<decimal>()
    let Float          = ComparisonValidator<float>()
    let Int            = ComparisonValidator<int>()
    let Int16          = ComparisonValidator<int16>()
    let Int64          = ComparisonValidator<int64>()
    let String         = StringValidator()
    let TimeSpan       = ComparisonValidator<TimeSpan>()