module SampleApp.Model

open Falco

[<CLIMutable>]
type Person =
    {
        First : string
        Last  : string 
    }

    static member FromReader (r : StringCollectionReader) =
        Ok {
            First = r?first.AsString()
            Last  = r?last.AsString()
        }

[<CLIMutable>]
type SearchQuery =
    {
        Frag : string
        Page : int option
        Take : int
    }

    static member FromReader (r : StringCollectionReader) =
        Ok {
            Frag = r?frag.AsString()
            Page = r.TryGetInt "page"   
            Take = r?take.AsInt()
        }