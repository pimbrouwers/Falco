module Falco.Security

module Crypto =    
    open System
    open System.Security.Cryptography
    open Microsoft.AspNetCore.Cryptography.KeyDerivation

    let fromBase64String str = 
        Convert.FromBase64String(str)

    let toBase64 bytes = 
        Convert.ToBase64String(bytes)
    
    let createSalt len =
        let salt = Array.zeroCreate<byte> len
        use rng = RandomNumberGenerator.Create()
        rng.GetBytes(salt)
        toBase64 salt

    let sha256 iterations len salt password =
        KeyDerivation.Pbkdf2(
            password,
            (fromBase64String salt),
            KeyDerivationPrf.HMACSHA256,
            iterations, 
            len)
        |> toBase64

module Xss =
    open FSharp.Control.Tasks.V2.ContextInsensitive
    open Microsoft.AspNetCore.Antiforgery    
    open Microsoft.AspNetCore.Http
    open Falco.ViewEngine
    
    // Generates a CSRF token using the Microsoft.AspNetCore.Antiforgery package,
    // which is fed into the provided handler
    let csrfTokenizer (handler : AntiforgeryTokenSet -> HttpHandler) : HttpHandler =
        fun (next: HttpFunc) (ctx : HttpContext) ->                
            let antiFrg = ctx.GetService<IAntiforgery>()
            (antiFrg.GetAndStoreTokens ctx |> handler) next ctx
    
    // Injects a newly generated CSRF token into a Falco.GiraffeViewEngine.XmlNode
    let csrfHtmlOut (view : AntiforgeryTokenSet -> XmlNode) : HttpHandler =            
        let handler token : HttpHandler =
            fun (next: HttpFunc) (ctx : HttpContext) ->              
                htmlOut (view token) next ctx
    
        csrfTokenizer handler  
    
    // Checks the presence and validity of CSRF token and calls invalidTokenHandler on failure
    // GET, HEAD, OPTIONS & TRACE always validate as true
    let requiresCsrfToken (invalidTokenHandler : HttpHandler) : HttpHandler =
        fun (next: HttpFunc) (ctx : HttpContext) ->                                
            task {
                let antiFrg = ctx.GetService<IAntiforgery>()        
                let! isValid = antiFrg.IsRequestValidAsync(ctx)
                return! 
                    match isValid with
                    | true  -> next ctx
                    | false -> (invalidTokenHandler shortCircuit) ctx
            }

