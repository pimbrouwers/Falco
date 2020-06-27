[<AutoOpen>]
module Falco.ModelBinding

open System.IO
open System.Text.Json
open Microsoft.AspNetCore.Http

type HttpContext with  
    /// Retrieve the HttpRequest body as string
    member this.GetBodyAsync () =
        async {
            use rd = new StreamReader(this.Request.Body)
            return! rd.ReadToEndAsync()
        } 
        |> Async.StartAsTask

    /// Retrieve IFormCollection from HttpRequest
    member this.GetFormAsync () = 
        async {
            return! this.Request.ReadFormAsync ()            
        }
        |> Async.StartAsTask

    /// Retrieve StringCollectionReader for IFormCollection from HttpRequest
    member this.GetFormReaderAsync () = 
        async {
            let! form = this.GetFormAsync ()
            return StringCollectionReader(form)
        }
        |> Async.StartAsTask

    /// Synchronously Retrieve StringCollectionReader for IFormCollection from HttpRequest
    member this.GetFormReader () =
        async {
            return! this.GetFormReaderAsync() 
        }
        |> Async.RunSynchronously

    /// Retrieve StringCollectionReader for IQueryCollection from HttpRequest
    member this.GetQueryReader () = 
        StringCollectionReader(this.Request.Query)


/// Map IFormCollection to record using provided `bind` function
let bindForm 
    (bind : StringCollectionReader -> 'a )     
    (success : 'a -> HttpHandler) : HttpHandler =    
    fun (next : HttpFunc) (ctx : HttpContext) ->  
        async {
            let! form = ctx.GetFormReaderAsync ()            
            return! (form |> bind |> success) next ctx
        }
        |> Async.StartAsTask

let bindJson<'a>
    (success : 'a -> HttpHandler) : HttpHandler =    
        fun (next : HttpFunc) (ctx : HttpContext) ->  
            async {                                
                let opt = JsonSerializerOptions()
                opt.AllowTrailingCommas <- true
                opt.PropertyNameCaseInsensitive <- true
                
                let! model = JsonSerializer.DeserializeAsync<'a>(ctx.Request.Body, opt).AsTask()
                
                return! (model |> success) next ctx
            }
            |> Async.StartAsTask

/// Attempt to map IFormCollection to record using provided `bind` function
let tryBindForm 
    (tryBind : StringCollectionReader -> Result<'a, string> ) 
    (error : string -> HttpHandler) 
    (success : 'a -> HttpHandler) : HttpHandler =    
    fun (next : HttpFunc) (ctx : HttpContext) ->  
        async {
            let! form = ctx.GetFormReaderAsync ()            
            return! 
                (match form |> tryBind with
                | Ok m      -> success m
                | Error msg -> error msg) next ctx
        }
        |> Async.StartAsTask

/// Attempt to map IQueryCollection to record using provided `bind` function
let bindQuery
    (bind : StringCollectionReader -> 'a)     
    (success : 'a -> HttpHandler) : HttpHandler =    
    fun (next : HttpFunc) (ctx : HttpContext) ->  
        (ctx.GetQueryReader() 
        |> bind
        |> success) next ctx
               
/// Attempt to map IQueryCollection to record using provided `bind` function
let tryBindQuery
    (tryBind : StringCollectionReader -> Result<'a, string> ) 
    (error : string -> HttpHandler) 
    (success : 'a -> HttpHandler) : HttpHandler =    
    fun (next : HttpFunc) (ctx : HttpContext) ->  
        (match ctx.GetQueryReader() |> tryBind with
        | Ok m      -> success m 
        | Error msg -> error msg) next ctx