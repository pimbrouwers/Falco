namespace Falco

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
    open System.Threading.Tasks
    open Microsoft.AspNetCore.Authentication
    open Microsoft.AspNetCore.Http
    open Falco.StringUtils

    /// Returns the current user (IPrincipal) or None.
    let getUser
        (ctx : HttpContext) =
        match ctx.User with
        | null -> None
        | _    -> Some ctx.User

    /// Authenticate the current request using the provided scheme.
    let authenticate
        (scheme : string)
        (ctx : HttpContext) =
        ctx.AuthenticateAsync(scheme)

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

    /// Attempts to return a specific claim from IPrincipal with a generic
    /// predicate.
    let tryFindClaim
        (predicate : Claim -> bool)
        (ctx : HttpContext) : Claim option =
        match getUser ctx with
        | None      -> None
        | Some user ->
            match user.Claims |> Seq.tryFind predicate with
            | None   -> None
            | Some claim -> Some claim

    /// Attempts to return specific claim from IPrincipal.
    let getClaim
        (claimType : string)
        (ctx : HttpContext) : Claim option =
        tryFindClaim (fun claim -> strEquals claim.Type claimType) ctx

    /// Attempts to return specific claim value from IPrincipal.
    let getClaimValue
        (claimType : string)
        (ctx : HttpContext) : string option =
        getClaim claimType ctx |> function
        | Some c -> Some c.Value
        | None -> None

    /// Returns bool if IPrincipal has specified scope.
    let hasScope
        (issuer : string)
        (scope : string)
        (ctx : HttpContext) : bool =
        let predicate (claim : Claim) = (strEquals claim.Issuer issuer) && (strEquals claim.Type "scope")

        tryFindClaim predicate ctx |> function
        | None       -> false
        | Some claim -> Array.contains scope (strSplit [|' '|] claim.Value)

    /// Establishes an authenticated context for the provide scheme and principal.
    let signIn
        (authScheme : string)
        (claimsPrincipal : ClaimsPrincipal)
        (ctx : HttpContext) : Task =
        ctx.SignInAsync(authScheme, claimsPrincipal)

    /// Establishes an authenticated context for the provide scheme, options and
    /// principal.
    let signInOptions
        (authScheme : string)
        (claimsPrincipal : ClaimsPrincipal)
        (options : AuthenticationProperties)
        (ctx : HttpContext) : Task =
        ctx.SignInAsync(authScheme, claimsPrincipal, options)

    /// Terminates authenticated context for provided scheme.
    let signOut
        (authScheme : string)
        (ctx : HttpContext) : Task =
        ctx.SignOutAsync(authScheme)

    /// Challenges the specified authentication scheme.
    ///
    /// An authentication challenge can be issued when an unauthenticated user
    /// requests an endpoint that requires authentication.
    ///
    /// Additional context may be provided via the given authentication
    /// properties.
    let challenge
        (authScheme : string)
        (properties : AuthenticationProperties)
        (ctx : HttpContext) : Task =
        ctx.ChallengeAsync(authScheme, properties)
