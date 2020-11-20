module Falco.Validation

open System
open Falco.StringUtils

type ValidationErrors = Map<string, string list>
type ValidationResult<'a> = Success of 'a | Failure of ValidationErrors
type Validator<'a> = string -> 'a -> ValidationResult<'a>

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

    /// Unpack ValidationResult and feed into validation function
    let apply (resultFn : ValidationResult<'a -> 'b>) (result : ValidationResult<'a>) : ValidationResult<'b> =
        match resultFn, result with
        | Success fn, Success x  -> fn x |> Success
        | Failure e, Success _   -> Failure e
        | Success _, Failure e   -> Failure e
        | Failure e1, Failure e2 -> Failure (ValidationErrors.merge e1 e2)  

    /// Unpack ValidationResult and apply inner value to function
    let bind (fn : 'a -> ValidationResult<'b>) (result : ValidationResult<'a>) : ValidationResult<'b> =
        match result with 
        | Success x -> fn x
        | Failure e -> Failure e

    /// Combine two ValidationResult
    let combine (a : ValidationResult<'a>) (b : ValidationResult<'a>) =
        match a, b with
        | Success a', Success _  -> Success a'
        | Failure e, Success _   -> Failure e
        | Success _, Failure e   -> Failure e
        | Failure e1, Failure e2 -> Failure (ValidationErrors.merge e1 e2)  

    /// Create a ValidationResult<'a> based on condition, yield
    /// error message if condition evaluates false
    let create condition value error : ValidationResult<'a> =
        if condition then Success value
        else error |> Failure

    /// Unpack ValidationResult, evaluate function if Success or return if Failure
    let map (fn : 'a -> 'b) (result : ValidationResult<'a>) : ValidationResult<'b> =
        match result with 
        | Success x -> fn x |> Success
        | Failure e -> Failure e

    /// Transform ValidationResult<'a> to Result<'a, ValidationErrors>
    let toResult (result : ValidationResult<'a>) : Result<'a, ValidationErrors> =
        match result with 
        | Success r -> Ok r
        | Failure e -> Error e

module Validator = 
    let compose (a : Validator<'a>) (b : Validator<'a>) =
        fun field value -> 
            ValidationResult.combine
                (a field value)
                (b field value)                  
            
[<RequireQualifiedAccess>]
module Validators =
    let private messageOrDefault (fieldName : string) (message : string option) (defaultMessage : unit -> string) =        
        let message' = message |> Option.defaultValue (defaultMessage ())        
        ValidationErrors.create fieldName [message']

    type EqualityValidator<'a when 'a : equality>() =                 
        member _.equals (equalTo : 'a) (message : string option) : Validator<'a> =
            fun (field : string) (value : 'a) -> 
                let defaultMessage () = sprintf "%s must be equal to %A" field equalTo
                ValidationResult.create (value = equalTo) value (messageOrDefault field message defaultMessage)

        member _.notEquals (notEqualTo : 'a) (message : string option) : Validator<'a> =
            fun (field : string) (value : 'a) -> 
                let defaultMessage () = sprintf "%s must not equal %A" field notEqualTo
                ValidationResult.create (value <> notEqualTo) value (messageOrDefault field message defaultMessage)    

    type ComparisonValidator<'a when 'a : comparison>() = 
        inherit EqualityValidator<'a>()

        member _.between (min : 'a) (max : 'a) (message : string option) : Validator<'a> =
            fun (field : string) (value : 'a) -> 
                let defaultMessage () = sprintf "%s must be between %A and %A" field min max
                ValidationResult.create (value >= min && value <= max) value (messageOrDefault field message defaultMessage)
                
        member _.greaterThan (min : 'a) (message : string option) : Validator<'a> =
            fun (field : string) (value : 'a) -> 
                let defaultMessage () = sprintf "%s must be greater than or equal to %A" field min
                ValidationResult.create (value > min) value (messageOrDefault field message defaultMessage)

        member _.lessThan (max : 'a) (message : string option) : Validator<'a> =
            fun (field : string) (value : 'a) -> 
                let defaultMessage () = sprintf "%s must be less than or equal to %A" field min
                ValidationResult.create (value < max) value (messageOrDefault field message defaultMessage)

    type StringValidator() =
        inherit EqualityValidator<string>() 

        member _.betweenLen (min : int) (max : int) (message : string option) : Validator<string> =
            fun (field : string) (value : string) ->
                let defaultMessage () = sprintf "%s must be between %i and %i characters" field min max
                ValidationResult.create (value.Length >= min && value.Length <= max) value (messageOrDefault field message defaultMessage)

        member _.empty (message : string option) : Validator<string> =
            fun (field : string) (value : string) ->
                let defaultMessage () = sprintf "%s must be empty" field
                ValidationResult.create (strEmpty value) value (messageOrDefault field message defaultMessage)

        member _.greaterThanLen (max : int) (message : string option) : Validator<string> =
            fun (field : string) (value : string) ->
                let defaultMessage () = sprintf "%s must not execeed %i characters" field max
                ValidationResult.create (value.Length < max) value (messageOrDefault field message defaultMessage)

        member _.lessThanLen (min : int) (message : string option) : Validator<string> =
            fun (field : string) (value : string) ->
                let defaultMessage () = sprintf "%s must be at least %i characters" field min
                ValidationResult.create (value.Length > min) value (messageOrDefault field message defaultMessage)

        member _.notEmpty (message : string option) : Validator<string> =
            fun (field : string) (value : string) ->
                let defaultMessage () = sprintf "%s must not be empty" field
                ValidationResult.create (strNotEmpty value) value (messageOrDefault field message defaultMessage)

        member _.pattern (pattern : string) (message : string option) : Validator<string> =
            fun (field : string) (value : string) ->
                let defaultMessage () = sprintf "%s must match pattern %s" field pattern
                ValidationResult.create (Text.RegularExpressions.Regex.IsMatch(value, pattern)) value (messageOrDefault field message defaultMessage)

    //let optional (validator : Validator<'a>) (field : string) (value : 'a option): ValidationResult<'a option> =  
    //    match value with
    //    | Some v -> validator field v |> Result.map (fun v -> Some v)
    //    | None   -> Success value

    //let required (validator : string option -> 'a -> ValidationResult<'a>) (fieldName : string) (message : string option) (value : 'a option) : ValidationResult<'a> =  
    //    let defaultMessage () = sprintf "%s is required" fieldName
    //    match value with
    //    | Some v -> validator message v
    //    | None   -> Error (messageOrDefault fieldName message defaultMessage)
            
    let DateTime       = ComparisonValidator<DateTime>()
    let DateTimeOffset = ComparisonValidator<DateTimeOffset>()
    let Decimal        = ComparisonValidator<decimal>()
    let Float          = ComparisonValidator<float>()
    let Int            = ComparisonValidator<int>()    
    let Int16          = ComparisonValidator<int16>()
    let Int64          = ComparisonValidator<int64>()
    let String         = StringValidator()
    let TimeSpan       = ComparisonValidator<TimeSpan>()

/// Custom operators for ValidationResult
module Operators =
    let (<*>) = ValidationResult.apply
    let (<!>) = ValidationResult.map
    let (<+>) = Validator.compose