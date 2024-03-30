namespace FalcoTutorial.Domain

module EntryModel =
    open System
    
    type Entry =
        { EntryId      : Guid
          HtmlContent  : string
          TextContent  : string }

    type EntrySummary =
        { EntryId   : Guid
          EntryDate : DateTime
          Summary   : string }

    let newEntry () =
        { EntryId = Guid.NewGuid()
          HtmlContent = String.Empty
          TextContent = String.Empty }
