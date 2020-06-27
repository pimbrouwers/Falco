module Falco.Security

module Crypto =    
    open System
    open System.Security.Cryptography
    open Microsoft.AspNetCore.Cryptography.KeyDerivation

    /// Make byte[] from Base64 string
    let fromBase64String (str : string) = 
        Convert.FromBase64String(str)

    /// Make Base64 string from byte[]
    let toBase64 (bytes : byte[]) = 
        Convert.ToBase64String(bytes)
    
    /// Generate a random int32 between range
    let randomInt min max = 
        Random().Next(min,max)

    /// Generate cryptographically-sound random salt
    /// Example: createSalt 16 (generates a 128-bit (i.e. 128 / 8) salt)
    let createSalt len =            
        let rndAry = Array.zeroCreate<byte> len
        use rng = RandomNumberGenerator.Create()
        rng.GetBytes(rndAry)        
        rndAry |> toBase64 

    /// Perform key derivation using the provided algorithm
    let pbkdf2 
        (prf : KeyDerivationPrf) 
        (iterations : int) 
        (numBytesRequested : int)
        (salt : string)
        (strToHash : string) =    
        KeyDerivation.Pbkdf2(
            strToHash,
            (fromBase64String salt),
            prf,
            iterations, 
            numBytesRequested)
        |> toBase64

    /// Perform key derivation using HMACSHA256
    let sha256 
        (iterations : int) 
        (numBytesRequested : int)
        (salt : string)
        (strToHash : string) =         
        pbkdf2 KeyDerivationPrf.HMACSHA256 iterations numBytesRequested salt strToHash
    
    /// Perform key derivation using HMACSHA512
    let sha512 
        (iterations : int) 
        (numBytesRequested : int)
        (salt : string)
        (strToHash : string) = 
        pbkdf2 KeyDerivationPrf.HMACSHA512 iterations numBytesRequested salt strToHash

module Xss =    
    open Microsoft.AspNetCore.Antiforgery    
    open Microsoft.AspNetCore.Http
    open Falco.ViewEngine
    
    type HttpContext with 
        /// Returns (and optional creates) csrf tokens for the current session
        member this.GetCsrfToken () =
            let antiFrg = this.GetService<IAntiforgery>()
            antiFrg.GetAndStoreTokens this
            
    /// Output an antiforgery <input type="hidden" />
    let antiforgeryInput (token : AntiforgeryTokenSet) =
        input [ 
                _type "hidden"
                _name token.FormFieldName
                _value token.RequestToken 
            ]

    /// Checks the presence and validity of CSRF token and calls invalidTokenHandler on failure
    /// GET, HEAD, OPTIONS & TRACE always validate as true
    let ifTokenValid (invalidTokenHandler : HttpHandler) : HttpHandler =
        fun (next: HttpFunc) (ctx : HttpContext) ->                                
            async {
                let antiFrg = ctx.GetService<IAntiforgery>()        
                let! isValid = antiFrg.IsRequestValidAsync(ctx)
                return! 
                    match isValid with
                    | true  -> next ctx
                    | false -> (invalidTokenHandler shortCircuit) ctx
            }
            |> Async.StartAsTask

    /// Generates a CSRF token using the Microsoft.AspNetCore.Antiforgery package,
    /// which is fed into the provided handler
    let csrfTokenizer (handler : AntiforgeryTokenSet -> HttpHandler) : HttpHandler =
        fun (next: HttpFunc) (ctx : HttpContext) ->                            
            (ctx.GetCsrfToken () |> handler) next ctx
    
    /// Injects a newly generated CSRF token into a Falco.XmlNode
    let csrfHtmlOut (view : AntiforgeryTokenSet -> XmlNode) : HttpHandler =            
        let handler token : HttpHandler =
            fun (next: HttpFunc) (ctx : HttpContext) ->              
                htmlOut (view token) next ctx
    
        csrfTokenizer handler  
