namespace Falco.Security

module Xss =
    open System.Threading.Tasks
    open Falco.Markup
    open Microsoft.AspNetCore.Antiforgery
    open Microsoft.AspNetCore.Http
    open Microsoft.Extensions.DependencyInjection

    /// Outputs an antiforgery <input type="hidden" />.
    let antiforgeryInput
        (token : AntiforgeryTokenSet) =
        Elem.input [
            Attr.type' "hidden"
            Attr.name token.FormFieldName
            Attr.value token.RequestToken ]

    /// Generates a CSRF token using the Microsoft.AspNetCore.Antiforgery
    /// package.
    let getToken (ctx : HttpContext) : AntiforgeryTokenSet =
        let antiFrg = ctx.RequestServices.GetRequiredService<IAntiforgery>()
        antiFrg.GetAndStoreTokens ctx

    /// Validates the Antiforgery token within the provided HttpContext.
    let validateToken (ctx : HttpContext) : Task<bool> =
        let antiFrg = ctx.RequestServices.GetRequiredService<IAntiforgery>()
        antiFrg.IsRequestValidAsync ctx

module Auth =
    open System.Security.Claims
    open Microsoft.AspNetCore.Http
    open Falco.StringUtils

    /// Returns the current user (IPrincipal) or None.
    let getUser
        (ctx : HttpContext) =
        match ctx.User with
        | null -> None
        | _    -> Some ctx.User

    /// Returns authentication status of IPrincipal, false on null.
    let isAuthenticated
        (ctx : HttpContext) : bool =
        let isAuthenciated (user : ClaimsPrincipal) =
            let identity = user.Identity
            match identity with
            | null -> false
            | _    -> identity.IsAuthenticated

        match getUser ctx with
        | None      -> false
        | Some user -> isAuthenciated user

    /// Returns bool if IPrincipal is in list of roles, false on None.
    let isInRole
        (roles : string list)
        (ctx : HttpContext) : bool =
        match getUser ctx with
        | None      -> false
        | Some user -> List.exists user.IsInRole roles

    /// Attempts to return claims from IPrincipal, empty seq on None.
    let getClaims
        (ctx : HttpContext) : Claim seq =
        match getUser ctx with
        | None      -> Seq.empty
        | Some user -> user.Claims

    /// Returns bool if IPrincipal has specified scope.
    let hasScope
        (issuer : string)
        (scope : string)
        (ctx : HttpContext) : bool =
        let predicate (claim : Claim) = (strEquals claim.Issuer issuer) && (strEquals claim.Type "scope")
        let claims = getClaims ctx
        match Seq.tryFind predicate claims with
        | None       -> false
        | Some claim -> Array.contains scope (strSplit [|' '|] claim.Value)
