[<AutoOpen>]
module Falco.ModelBinding

open System.IO
open System.Text.Json
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http

type HttpContext with  
    /// Retrieve the HttpRequest body as string
    member this.GetBodyAsync () =
        task {
            use rd = new StreamReader(this.Request.Body)
            return! rd.ReadToEndAsync()
        }

    /// Retrieve IFormCollection from HttpRequest
    member this.GetFormAsync () = 
        task {
            return! this.Request.ReadFormAsync ()            
        }

    /// Retrieve StringCollectionReader for IFormCollection from HttpRequest
    member this.GetFormReaderAsync () = 
        task {
            let! form = this.GetFormAsync ()
            return StringCollectionReader(form)
        }        

    /// Synchronously Retrieve StringCollectionReader for IFormCollection from HttpRequest
    member this.GetFormReader () = 
        this.GetFormReaderAsync().Result

    /// Retrieve StringCollectionReader for IQueryCollection from HttpRequest
    member this.GetQueryReader () = 
        StringCollectionReader(this.Request.Query)

/// Map IFormCollection to record using provided `bind` function
let bindForm 
    (bind : StringCollectionReader -> 'a )     
    (success : 'a -> HttpHandler) : HttpHandler =    
    fun (next : HttpFunc) (ctx : HttpContext) ->  
        task {
            let! form = ctx.GetFormReaderAsync ()            
            return! (form |> bind |> success) next ctx
        }

let bindJson<'a>
    (success : 'a -> HttpHandler) : HttpHandler =    
        fun (next : HttpFunc) (ctx : HttpContext) ->  
            task {                                
                let opt = JsonSerializerOptions()
                opt.AllowTrailingCommas <- true
                opt.PropertyNameCaseInsensitive <- true
                
                let! model = JsonSerializer.DeserializeAsync<'a>(ctx.Request.Body, opt).AsTask()
                
                return! (model |> success) next ctx
            }

/// Attempt to map IFormCollection to record using provided `bind` function
let tryBindForm 
    (tryBind : StringCollectionReader -> Result<'a, string> ) 
    (error : string -> HttpHandler) 
    (success : 'a -> HttpHandler) : HttpHandler =    
    fun (next : HttpFunc) (ctx : HttpContext) ->  
        task {
            let! form = ctx.GetFormReaderAsync ()            
            return! 
                (match form |> tryBind with
                | Ok m      -> success m
                | Error msg -> error msg) next ctx
        }

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