module Falco.Security.Xss

open System.Threading.Tasks
open Falco.Extensions
open Falco.Markup
open Microsoft.AspNetCore.Antiforgery    
open Microsoft.AspNetCore.Http

/// Output an antiforgery <input type="hidden" />
let antiforgeryInput 
    (token : AntiforgeryTokenSet) =
    Elem.input [ 
            Attr.type' "hidden"
            Attr.name token.FormFieldName
            Attr.value token.RequestToken 
        ]

/// Generates a CSRF token using the Microsoft.AspNetCore.Antiforgery package
let getToken 
    (ctx : HttpContext) : AntiforgeryTokenSet =
    ctx.GetCsrfToken()

/// Validate the Antiforgery token within the provided HttpContext
let validateToken
    (ctx : HttpContext) : Task<bool> =        
    ctx.ValidateCsrfToken()

