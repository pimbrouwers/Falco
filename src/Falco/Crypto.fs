module Falco.Security.Crypto

open System
open System.Text
open System.Security.Cryptography

/// Make byte[] from Base64 string
let bytesFromBase64 (str : string) =
    Convert.FromBase64String str

/// Make Base64 string from byte[]
let bytesToBase64 (bytes : byte[]) =
    Convert.ToBase64String bytes

/// Generate a random int32 between range
let randomInt min max =
    RandomNumberGenerator.GetInt32(min,max)

/// Generate cryptographically-sound random salt
/// Example: createSalt 16 (generates a 128-bit (i.e. 128 / 8) salt)
let createSalt len =
    let rndAry = Array.zeroCreate<byte> len
    use rng = RandomNumberGenerator.Create()
    rng.GetBytes rndAry
    rndAry |> bytesToBase64

/// Perform key derivation using the provided algorithm
let pbkdf2
    (algo : HashAlgorithmName)
    (iterations : int)
    (numBytesRequested : int)
    (salt : byte[])
    (input : byte[]) =
    let pbkdf2 = new Rfc2898DeriveBytes(input, salt, iterations, algo)
    let bytes = pbkdf2.GetBytes numBytesRequested
    bytesToBase64 bytes
    
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
        (Encoding.UTF8.GetBytes salt)
        (Encoding.UTF8.GetBytes strToHash)

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
        (Encoding.UTF8.GetBytes salt)
        (Encoding.UTF8.GetBytes strToHash)
