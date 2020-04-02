[<AutoOpen>]
module Falco.Request

open Microsoft.AspNetCore.Http

type HttpContext with    
    member this.GetFormValues () =
        this.Request.Form
        |> Seq.map (fun (kvp) -> kvp.Key, kvp.Value)
        |> Map.ofSeq

    member this.TryGetFormValue (key : string) =
        let parseForm = tryParseWith this.Request.Form.TryGetValue
        match parseForm key with 
        | Some v -> Some v
        | None   -> None

    member this.GetRouteValues () =
        this.Request.RouteValues
        |> Seq.map (fun kvp -> kvp.Key, toStr kvp.Value)
        |> Map.ofSeq
        
    member this.TryGetRouteValue (key : string) =
        let parseRoute = tryParseWith this.Request.RouteValues.TryGetValue             
        match parseRoute key with
        | Some v -> Some (toStr v)
        | None   -> None

    member this.GetService<'a> () =
        let t = typeof<'a>
        match this.RequestServices.GetService t with
        | null    -> raise (InvalidDependencyException t.Name)
        | service -> service :?> 'a