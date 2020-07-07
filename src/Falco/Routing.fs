[<AutoOpen>]
module Falco.Routing

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Falco.StringParser
open Falco.StringUtils

/// Constructor for HttpEndpoint
let route 
    (verb : HttpVerb) 
    (pattern : string) 
    (handler : HttpHandler) : HttpEndpoint =
    { 
        Pattern = pattern
        Verb  = verb
        Handler = handler
    }

/// HttpEndpoint constructor that matches any HttpVerb
let any : MapHttpEndpoint = 
    fun (pattern : string) 
        (handler : HttpHandler) ->
        route ANY pattern handler
    
/// GET HttpEndpoint constructor
let get : MapHttpEndpoint = 
    fun (pattern : string) 
        (handler : HttpHandler) ->
        route GET pattern handler

/// HEAD HttpEndpoint constructor
let head : MapHttpEndpoint = 
    fun (pattern : string) 
        (handler : HttpHandler) ->
        route HEAD pattern handler

/// POST HttpEndpoint constructor
let post : MapHttpEndpoint = 
    fun (pattern : string) 
        (handler : HttpHandler) ->
        route POST pattern handler

/// PUT HttpEndpoint constructor
let put : MapHttpEndpoint = 
    fun (pattern : string) 
        (handler : HttpHandler) ->
        route PUT pattern handler

/// PATCH HttpEndpoint constructor
let patch : MapHttpEndpoint = 
    fun (pattern : string) 
        (handler : HttpHandler) ->
        route PATCH pattern handler

/// DELETE HttpEndpoint constructor
let delete : MapHttpEndpoint = 
    fun (pattern : string) 
        (handler : HttpHandler) ->
        route DELETE pattern handler

/// OPTIONS HttpEndpoint constructor
let options : MapHttpEndpoint = 
    fun (pattern : string) 
        (handler : HttpHandler) ->
        route OPTIONS pattern handler

/// TRACE HttpEndpoint construct
let trace : MapHttpEndpoint = 
    fun (pattern : string) 
        (handler : HttpHandler) ->
        route TRACE pattern handler