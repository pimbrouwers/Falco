[<AutoOpen>]
module AppName.Common

type ApiError = 
    {
        Code    : int 
        Message : string list
    }

/// Internal URLs
[<RequireQualifiedAccess>]
module Urls = 
    let ``/`` = "/"
    let ``/value/create`` = "/value/create"