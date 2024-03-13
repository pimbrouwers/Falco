namespace FalcoTutorial

open System

module Domain =
    module EntryModel =
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

module Infrastructure =
    open System.Data

    type IDbConnection with
        member x.CreateCommandText(commandText : string) =
            let cmd = x.CreateCommand()
            cmd.CommandType <- CommandType.Text
            cmd.CommandText <- commandText
            cmd

    type IDbCommand with
        member x.AddParameter(name : string, value : obj) : unit =
            let p = x.CreateParameter()
            p.ParameterName <- name
            if isNull value then p.Value <- DBNull.Value
            else p.Value <- value
            x.Parameters.Add p |> ignore

        member x.Execute() : unit =
            x.ExecuteNonQuery() |> ignore

        member x.Query(map : IDataRecord -> 'a) : 'a list =
            use rd = x.ExecuteReader()
            [ while rd.Read() do
                yield map rd ]

    type IDbConnectionFactory =
        abstract member Create : unit -> IDbConnection

    module SqliteConnectionFactory =
        open Microsoft.Data.Sqlite

        let create (connectionString : string) =
            { new IDbConnectionFactory with
                member _.Create() =
                    let conn = new SqliteConnection(connectionString)
                    conn.Open()
                    conn }

    module EntryStore =
        open Domain.EntryModel

        let save (conn : IDbConnection) (entry : Entry) : unit =
            use cmd = conn.CreateCommandText("
                INSERT OR REPLACE INTO entry (entry_id, html_content, text_content, modified_date)
                VALUES (@entry_id, @html_content, @text_content, DATETIME('now'));")

            cmd.AddParameter ("entry_id", entry.EntryId)
            cmd.AddParameter ("html_content", entry.HtmlContent)
            cmd.AddParameter ("text_content", entry.TextContent)

            cmd.Execute()

        let get (conn : IDbConnection) (entryId : Guid) : Entry option =
            use cmd = conn.CreateCommandText("
                SELECT    entry_id
                        , html_content
                        , text_content
                FROM      entry
                WHERE     entry_id = @entry_id")

            cmd.AddParameter ("entry_id", entryId)

            cmd.Query (fun rd ->
                { EntryId = rd.GetGuid(rd.GetOrdinal("entry_id"))
                  HtmlContent = rd.GetString(rd.GetOrdinal("html_content"))
                  TextContent = rd.GetString(rd.GetOrdinal("text_content")) })
            |> Seq.tryHead

        let getAll (conn : IDbConnection) : EntrySummary list =
            use cmd = conn.CreateCommandText("
                SELECT    entry_id
                        , modified_date AS entry_date
                        , SUBSTR(text_content, 0, 50) AS summary
                FROM      entry
                ORDER BY  modified_date DESC")

            cmd.Query (fun rd ->
                { EntryId = rd.GetGuid(rd.GetOrdinal("entry_id"))
                  EntryDate = rd.GetDateTime(rd.GetOrdinal("entry_date"))
                  Summary = rd.GetString(rd.GetOrdinal("summary")) })

module Service =
    open Infrastructure

    module EntryService =
        open Domain.EntryModel

        let getAll (dbConnection : IDbConnectionFactory) =
            use conn = dbConnection.Create ()
            EntryStore.getAll conn

        let get (dbConnection : IDbConnectionFactory) (id : Guid) =
            use conn = dbConnection.Create ()
            EntryStore.get conn id

        let save (dbConnection : IDbConnectionFactory) (input : Entry) =
            use conn = dbConnection.Create ()
            EntryStore.save conn input

module Web =
    open Domain

    /// Kestrel endpoint routes
    module Route =
        let index = "/"
        let notFound = "/not-found"
        let entryCreate = "/entry/create"
        let entryEdit = "/entry/edit/{entry_id}"

    /// Url factories
    module Urls =
        let index = Route.index
        let notFound = Route.notFound
        let entryCreate = Route.entryCreate
        let entryEdit (entryId : Guid) = Route.entryEdit.Replace("{entry_id}", entryId.ToString("d"))

    /// Shared markup
    module View =
        open Falco.Markup

        /// Master layout which accepts a title and content for <body></body>
        let layout (title : string) (content : XmlNode list) =
            Elem.html [ Attr.lang "en"; Attr.class' "bg-dark-gray" ] [
                Elem.head [] [
                    Elem.meta  [ Attr.charset "UTF-8" ]
                    Elem.meta  [ Attr.httpEquiv "X-UA-Compatible"; Attr.content "IE=edge, chrome=1" ]
                    Elem.meta  [ Attr.name "viewport"; Attr.content "width=device-width, initial-scale=1" ]
                    Elem.title [] [ Text.rawf "%s | FalcoJournal" title ]
                    Elem.link  [ Attr.href "/tachyons.css"; Attr.rel "stylesheet" ] ]

                Elem.body [ Attr.class' "mw7 center ph3 f4-l georgia white-90" ] [
                    Elem.main [] content
                    Elem.script [ Attr.src "/index.js"; Attr.type' "text/javascript" ] [] ] ]

        /// Page title as <h1></h1>
        let pageTitle (title : string) =
            Elem.h1 [ Attr.class' "pb3 code tc white-90" ] [ Text.raw title ]

        /// Top bar with pluggable actions
        let topBar (actions : XmlNode list) =
            Elem.header [ Attr.class' "pv4" ] [
                Elem.nav [ Attr.class' "flex items-center" ] [
                    Elem.a [ Attr.class' "f4 f3-l white-90 no-underline"
                             Attr.href Urls.index ] [ Text.raw "Falco Journal" ]
                    Elem.div [ Attr.class' "flex-grow-1 tr" ] actions ] ]

        /// Button link elements
        module Buttons =
            let solidGray label url =
                let attr =  [
                    Attr.href url
                    Attr.class' "dib pa2 bg-light-gray dark-gray no-underline bn br1" ]

                Elem.a attr [ Text.raw label ]

            let solidWhite label url =
                let attr =  [
                    Attr.href url
                    Attr.class' "dib pa2 ph3-l white-90 no-underline bn br1" ]

                Elem.a attr [ Text.raw label ]

        /// Form elements
        module Forms =
            let inputText attr =
                // safely combine custom attributes with defaults
                let defaultAttr = [
                    Attr.type' "text"
                    Attr.class' "pa2 ba b--silver br1" ]

                let mergedAttr = attr |> Attr.merge defaultAttr
                Elem.input mergedAttr

            let submit attr =
                // safely combine custom attributes with defaults
                let defaultAttr = [
                    Attr.type' "submit"
                    Attr.class' "dib pa2 bg-light-gray dark-gray bn br1"]

                let mergedAttr = attr |> Attr.merge defaultAttr
                Elem.input mergedAttr

    module ErrorController =
        open Falco
        open View

        let badRequest : HttpHandler =
            Response.withStatusCode 400
            >> Response.ofPlainText "Bad request"

        let notFound : HttpHandler =
            let html =
                layout "Not Found" [
                    pageTitle "Not found" ]

            Response.withStatusCode 404
            >> Response.ofHtml html

    module EntryViews =
        open Falco.Markup
        open Falco.Security
        open Domain.EntryModel
        open View

        let index (entries : EntrySummary list) =
            let title = "Journal Entries"

            layout title [
                topBar [ Buttons.solidGray "New Entry" Urls.entryCreate ]
                pageTitle title

                Elem.div [] [
                    if entries.Length = 0 then
                        Elem.i [] [ Text.raw "No entries have been added" ]
                    else
                        for e in entries do
                            Elem.a [ Attr.href (Urls.entryEdit e.EntryId); Attr.class' "db mb4 no-underline white-90" ] [
                                Elem.div [ Attr.class' "mb1 f6 code moon-gray" ] [ Text.raw (e.EntryDate.ToString("yyyy/MM/dd HH:MM")) ]
                                Elem.div [] [ Text.raw e.Summary ]
                                Elem.div [ Attr.class' "w1 mt3 bt b--moon-gray" ] [] ] ] ]

        let save action entry token =
            let title = "A place for your thoughts..."

            let htmlContent =
                match entry.HtmlContent with
                | str when String.IsNullOrWhiteSpace(str) -> Elem.li [] []
                | _ -> Text.raw entry.HtmlContent

            layout title [
                topBar [
                    Forms.submit [ Attr.value "Save Entry"; Attr.form "entry-editor" ]
                    Buttons.solidWhite "Cancel" Urls.index ]

                pageTitle title

                Elem.form [ Attr.id "entry-editor"; Attr.class' "lh-copy"; Attr.method "post"; Attr.action action ] [
                    Xss.antiforgeryInput token
                    Elem.ul [
                        Attr.id "bullet-editor"
                        Attr.class' "mh0 pl3 outline-0"
                        Attr.create "contenteditable" "true" ] [
                            htmlContent ]
                    Elem.input [ Attr.type' "hidden"; Attr.name "entry_id"; Attr.value (string entry.EntryId) ]
                    Elem.input [ Attr.type' "hidden"; Attr.name "html_content"; Attr.id "bullet-editor-html" ]
                    Elem.input [ Attr.type' "hidden"; Attr.name "text_content"; Attr.id "bullet-editor-text" ] ] ]

    module EntryController =
        open Falco
        open Domain.EntryModel
        open Infrastructure
        open Service

        /// GET /
        let index (dbConnection : IDbConnectionFactory) : HttpHandler =
            let entries = EntryService.getAll dbConnection
            Response.ofHtml (EntryViews.index entries)

        /// GET /entry/create
        let create : HttpHandler = fun ctx ->
            let newEntry = EntryModel.newEntry ()
            let view = EntryViews.save Urls.entryCreate newEntry
            Response.ofHtmlCsrf view ctx

        /// GET /entry/edit/{id}
        let edit (dbConnection : IDbConnectionFactory) : HttpHandler =
            let readRoute (route : RouteCollectionReader) =
                route.TryGetGuid "entry_id"

            let handle (input : Guid option) =
                match input with
                | Some entryId ->
                    match EntryService.get dbConnection entryId with
                    | Some entry ->
                        let html = EntryViews.save (Urls.entryEdit entryId) entry
                        Response.ofHtmlCsrf html
                    | None ->
                        ErrorController.notFound
                | None ->
                    Response.redirectTemporarily Urls.notFound

            Request.mapRoute readRoute handle

        /// POST /entry/create, /entry/edit/{id}
        let save (dbConnection : IDbConnectionFactory) : HttpHandler =
            let readForm (form : FormCollectionReader) =
                { EntryId = form.GetGuid "entry_id"
                  HtmlContent = form.GetString "html_content"
                  TextContent = form.GetString "text_content" }

            let handle (input : Entry) =
                EntryService.save dbConnection input
                Response.redirectTemporarily Urls.index

            Request.mapForm readForm handle

module Program =
    open Falco
    open Falco.Routing
    open Microsoft.AspNetCore.Builder
    open Microsoft.Extensions.Configuration
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.Extensions.Hosting
    open Microsoft.Extensions.Logging
    open Infrastructure
    open Web

    type App(dbConnection : IDbConnectionFactory) =
        member _.Endpoints =
            seq {
                get Route.index (EntryController.index dbConnection)
                get Route.notFound ErrorController.notFound
                all Route.entryCreate [
                    GET, EntryController.create
                    POST, (EntryController.save dbConnection) ]
                all Route.entryEdit [
                    GET, (EntryController.edit dbConnection)
                    POST, (EntryController.save dbConnection) ]
            }

        member _.NotFound =
            Response.withStatusCode 404 >> Response.ofPlainText "Not Found"

    [<EntryPoint>]
    let main args =
        Falco(args)
        |> Falco.addService AntiforgeryServiceCollectionExtensions.AddAntiforgery
        |> Falco.addSingleton'<IDbConnectionFactory>(fun conf _ ->
            conf.GetConnectionString("Default")
            |> SqliteConnectionFactory.create)
        |> Falco.middleware StaticFileExtensions.UseStaticFiles
        |> Falco.get Route.index (Falco.plug<IDbConnectionFactory> EntryController.index)
        |> Falco.get Route.notFound ErrorController.notFound
        |> Falco.all Route.entryCreate [
            GET, EntryController.create
            POST, (Falco.plug<IDbConnectionFactory> EntryController.save) ]
        |> Falco.all Route.entryEdit [
            GET, (Falco.plug<IDbConnectionFactory> EntryController.edit)
            POST, (Falco.plug<IDbConnectionFactory> EntryController.save) ]
        |> Falco.notFound (Response.withStatusCode 404 >> Response.ofPlainText "Not Found")
        |> Falco.run

        0