[<AutoOpen>]
module SampleApp.Common

[<CLIMutable>]
type Person =
    {
        First : string
        Last  : string 
    }

[<CLIMutable>]
type SearchQuery =
    {
        Frag : string
        Page : int option
        Take : int
    }