namespace Falco.Security

module Crypto =
    open System
    open System.Text
    open System.Security.Cryptography

    /// Makes byte[] from Base64 string.
    let private bytesFromBase64 (str : string) =
        Convert.FromBase64String str

    /// Makes Base64 string from byte[].
    let private bytesToBase64 (bytes : byte[]) =
        Convert.ToBase64String bytes

    /// Generates a random Int32 between range.
    let randomInt min max =
        RandomNumberGenerator.GetInt32(min,max)

    /// Generates cryptographically-sound random salt.
    ///
    /// Example: `createSalt 16 (generates a 128-bit (i.e. 128 / 8) salt).`
    let createSalt len =
        let rndAry = Array.zeroCreate<byte> len
        use rng = RandomNumberGenerator.Create()
        rng.GetBytes rndAry
        rndAry |> bytesToBase64

    /// Performs key derivation using the provided algorithm.
    let pbkdf2
        (algo : HashAlgorithmName)
        (iterations : int)
        (numBytesRequested : int)
        (salt : byte[])
        (input : byte[]) =
        let pbkdf2 = new Rfc2898DeriveBytes(input, salt, iterations, algo)
        let bytes = pbkdf2.GetBytes numBytesRequested
        bytesToBase64 bytes

    /// Performs PBKDF2 key derivation using HMACSHA256.
    let sha256
        (iterations : int)
        (numBytesRequested : int)
        (salt : string)
        (strToHash : string) =
        pbkdf2
            HashAlgorithmName.SHA256
            iterations
            numBytesRequested
            (Encoding.UTF8.GetBytes salt)
            (Encoding.UTF8.GetBytes strToHash)

    /// Performs key derivation using HMACSHA512.
    let sha512
        (iterations : int)
        (numBytesRequested : int)
        (salt : string)
        (strToHash : string) =
        pbkdf2
            HashAlgorithmName.SHA512
            iterations
            numBytesRequested
            (Encoding.UTF8.GetBytes salt)
            (Encoding.UTF8.GetBytes strToHash)

module Xss =
    open System.Threading.Tasks
    open Falco.Markup
    open Microsoft.AspNetCore.Antiforgery
    open Microsoft.AspNetCore.Http
    open Falco

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
        let antiFrg = ctx.GetService<IAntiforgery>()
        antiFrg.GetAndStoreTokens ctx

    /// Validates the Antiforgery token within the provided HttpContext.
    let validateToken (ctx : HttpContext) : Task<bool> =
        let antiFrg = ctx.GetService<IAntiforgery>()
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
