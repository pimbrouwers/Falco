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
        
        member this.equals (equalTo : 'a) (message : string option) : Validator<'a> =
                let defaultMessage () = sprintf "Value must be equal to %A" equalTo
                fun (value) -> ValidationResult.create (value = equalTo) value (messageOrDefault message defaultMessage)

        member this.notEquals (notEqualTo : 'a) (message : string option) : Validator<'a> =
                let defaultMessage () = sprintf "Value must not equal %A" notEqualTo
                fun (value) -> ValidationResult.create (value <> notEqualTo) value (messageOrDefault message defaultMessage)    

    type ComparisonValidator<'a when 'a : comparison>() = 
        inherit EqualityValidator<'a>()

        member this.between (min : 'a) (max : 'a) (message : string option) : Validator<'a> =
            let defaultMessage () = sprintf "Value must be between %A and %A" min max
            fun (value) -> ValidationResult.create (value >= min && value <= max) value (messageOrDefault message defaultMessage)
                
        member this.greaterThan (min : 'a) (message : string option) : Validator<'a> =
            let defaultMessage () = sprintf "Value must be greater than or equal to %A" min
            fun (value) -> ValidationResult.create (value >= min) value (messageOrDefault message defaultMessage)

        member this.lessThan (max : 'a) (message : string option) : Validator<'a> =
            let defaultMessage () = sprintf "Value must be less than or equal to %A" min
            fun (value) -> ValidationResult.create (value <= max) value (messageOrDefault message defaultMessage)

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

    //module String =
    //    let minLength (min : int) (fieldName : string) (value : string) =
    //        ValidationResult.create (value.Length >= min) value (Messages.gteSuffix (Some "characters") fieldName min)
