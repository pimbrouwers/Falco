module Falco.Security

module Crypto =    
    open System
    open System.Security.Cryptography
        
    /// Make byte[] from Base64 string
    let bytesFromBase64 (str : string) = 
        Convert.FromBase64String(str)
    
    /// Make Base64 string from byte[]
    let bytesToBase64 (bytes : byte[]) = 
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
        rndAry |> bytesToBase64 

    /// Perform key derivation using the provided algorithm
    let pbkdf2 
        (algo : HashAlgorithmName) 
        (iterations : int) 
        (numBytesRequested : int)
        (salt : byte[])
        (input : byte[]) =    
        let pbkdf2 = new Rfc2898DeriveBytes(input, salt, iterations, algo)
            
        pbkdf2.GetBytes(numBytesRequested)
        |> bytesToBase64

    /// Perform PBKDF2 key derivation using HMACSHA256
    let sha256 
        (iterations : int) 
        (numBytesRequested : int)
        (salt : string)
        (strToHash : string) =                
        pbkdf2 
            HashAlgorithmName.SHA256
            iterations 
            numBytesRequested 
            (Text.Encoding.UTF8.GetBytes salt)
            (Text.Encoding.UTF8.GetBytes strToHash)
    
    /// Perform key derivation using HMACSHA512
    let sha512 
        (iterations : int) 
        (numBytesRequested : int)
        (salt : string)
        (strToHash : string) = 
        pbkdf2 
            HashAlgorithmName.SHA512
            iterations 
            numBytesRequested 
            (Text.Encoding.UTF8.GetBytes salt)
            (Text.Encoding.UTF8.GetBytes strToHash)

module Xss =    
    open System.Threading.Tasks
    open Falco.Markup
    open Microsoft.AspNetCore.Antiforgery    
    open Microsoft.AspNetCore.Http
    
    type HttpContext with 
        /// Returns (and optional creates) csrf tokens for the current session
        member this.GetCsrfToken () =
            let antiFrg = this.GetService<IAntiforgery>()
            antiFrg.GetAndStoreTokens this

         /// Checks the presence and validity of CSRF token 
         member this.ValidateCsrfToken () =
            let antiFrg = this.GetService<IAntiforgery>()        
            antiFrg.IsRequestValidAsync this
    
    /// Output an antiforgery <input type="hidden" />
    let antiforgeryInput 
        (token : AntiforgeryTokenSet) =
        Elem.input [ 
                Attr.type' "hidden"
                Attr.name token.FormFieldName
                Attr.value token.RequestToken 
            ]
                
    /// GET, HEAD, OPTIONS & TRACE always validate as true
    let isTokenValid 
        (ctx : HttpContext) : Task<bool> =        
            ctx.ValidateCsrfToken()

    ///// Generates a CSRF token using the Microsoft.AspNetCore.Antiforgery package,
    ///// which is fed into the provided handler
    //let csrfTokenizer (handler : AntiforgeryTokenSet -> HttpHandler) : HttpHandler =
    //    fun (next: HttpFunc) (ctx : HttpContext) ->                            
    //        (ctx.GetCsrfToken () |> handler) next ctx
    
    ///// Injects a newly generated CSRF token into a Falco.XmlNode
    //let csrfHtmlOut (view : AntiforgeryTokenSet -> XmlNode) : HttpHandler =            
    //    let handler token : HttpHandler =
    //        fun (next: HttpFunc) (ctx : HttpContext) ->              
    //            htmlOut (view token) next ctx
    
    //    csrfTokenizer handler  
