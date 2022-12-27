namespace FalcoTutorial

open System

type Entry =
    { EntryId      : Guid
      HtmlContent  : string
      TextContent  : string
      EntryDate    : DateTime }

type EntrySummary =
    { EntryId   : Guid
      EntryDate : DateTime
      Summary   : string }

module Db =
    open System.Data

    type IDbConnectionFactory =
        abstract member CreateConnection : unit -> IDbConnection

    type IDbCommand with
        member x.AddParameter(name : string, value : obj) : unit =
            let p = x.CreateParameter()
            p.ParameterName <- name
            if isNull value then p.Value <- DBNull.Value
            else p.Value <- value

        member private x.Prepare() : unit =
            if x.Connection.State = ConnectionState.Closed then
                x.Connection.Open()

        member x.Execute() : unit =
            x.Prepare()
            x.ExecuteNonQuery() |> ignore

        member x.Query(map : IDataRecord -> 'a) : 'a list =
            x.Prepare()
            use rd = x.ExecuteReader()
            [ while rd.Read() do yield map rd ]

    module EntryStore =
        let save (conn : IDbConnection) (entry : Entry) : unit =
            use cmd = conn.CreateCommand()
            cmd.CommandText <- "
                INSERT OR REPLACE INTO entry (entry_id, html_content, text_content, entry_date, modified_date)
                VALUES (@entry_id, @html_content, @text_content, DATETIME('now'), DATETIME('now'));"

            cmd.AddParameter("entry_id", entry.EntryId)
            cmd.AddParameter("html_content", entry.HtmlContent)
            cmd.AddParameter("text_content", entry.TextContent)

            cmd.Execute()

        let getAll (conn : IDbConnection) : EntrySummary list =
            use cmd = conn.CreateCommand()
            cmd.CommandText <- "
                SELECT    entry_id
                        , DATETIME(entry_date) AS entry_date
                        , SUBSTR(text_content, 0, 50) AS summary
                FROM      entry
                ORDER BY  DATETIME(entry_date) DESC, DATETIME(modified_date) DESC"

            cmd.Query (fun rd ->
                { EntryId = rd.GetGuid(rd.GetOrdinal("entry_id"))
                  EntryDate = rd.GetDateTime(rd.GetOrdinal("entry_date"))
                  Summary = rd.GetString(rd.GetOrdinal("summary")) })

module App =
    open Db

    module Urls =
        let index = "/"

        let entryCreate = "/entry/create"
        let entryEdit entryId = sprintf "/entry/edit/%O" entryId

    module UI =
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

    module EntryViews =
        open Falco.Markup
        open UI

        let index (entries : EntrySummary list) =
            let title = "Journal Entries"

            layout title [
                topBar [ Buttons.solidGray "New Entry" Urls.entryCreate ]
                pageTitle title

                Elem.div [] [
                    for e in entries do
                        Elem.a [ Attr.href (Urls.entryEdit e.EntryId); Attr.class' "db mb4 no-underline white-90" ] [
                            Elem.div [ Attr.class' "mb1 f6 code moon-gray" ] [
                                Text.raw (e.EntryDate.ToString("yyyy/MM/dd HH:MM")) ]
                            Elem.div [] [
                                Text.raw e.Summary ]
                            Elem.div [ Attr.class' "w1 mt3 bt b--moon-gray" ] [] ] ]
            ]

        let save action entry =
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

                Elem.form [
                    Attr.id "entry-editor"
                    Attr.class' "lh-copy"
                    Attr.method "post"
                    Attr.action action ] [

                    Elem.ul [
                        Attr.id "bullet-editor"
                        Attr.class' "mh0 pl3 outline-0"
                        Attr.create "contenteditable" "true" ] [
                            htmlContent ]

                    Elem.input [ Attr.type' "hidden"; Attr.name "entry_id"; Attr.value (string entry.EntryId) ]
                    Elem.input [ Attr.type' "hidden"; Attr.name "html_content" ]
                    Elem.input [ Attr.type' "hidden"; Attr.name "text_content" ]
                    Elem.input [ Attr.type' "hidden"; Attr.name "entry_date"; Attr.value (entry.EntryDate.ToString("yyyy-MM-dd"))]
                ]
            ]

    module EntryController =
        open Falco

        /// GET /
        let index : HttpHandler =
            Services.inject<IDbConnectionFactory>(fun db ->
                use conn = db.CreateConnection()
                let entries = EntryStore.getAll conn
                Response.ofHtml (EntryViews.index entries))

        /// GET /entry/create
        let create : HttpHandler =
            let newEntry : Entry =
                { EntryId = Guid.NewGuid()
                  HtmlContent = String.Empty
                  TextContent = String.Empty
                  EntryDate = DateTime.Now }

            let view = EntryViews.save Urls.entryCreate newEntry

            Response.ofHtml view

        /// GET /entry/edit/{id}
        let edit : HttpHandler =
            Response.ofPlainText "Entry edit"

        /// POST /entry/create, /entry/edit/{id}
        let save : HttpHandler =
            Response.ofPlainText "Entry save"

module Program =
    open Falco
    open Falco.Routing
    open Falco.HostBuilder
    open Microsoft.AspNetCore.Builder
    open Microsoft.Extensions.Configuration
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.Data.Sqlite
    open Db
    open App

    type DbConnectionFactory (connectionString : string) =
        interface IDbConnectionFactory with
            member _.CreateConnection () =
                let conn = new SqliteConnection(connectionString)
                conn.Open()
                conn

    module Endpoints =
        let index = Urls.index

        let entryCreate = Urls.entryCreate
        let entryEdit = Urls.entryEdit "{id}"

    [<EntryPoint>]
    let main args =
        /// App configuration, loaded on startup
        let config = configuration [||] {
            required_json "appsettings.json"
        }

        let connectionString = config.GetConnectionString("Default")

        if String.IsNullOrWhiteSpace(connectionString) then
            failwith "Invalid connection string [EMPTY]"

        let dbConnectionService (svc : IServiceCollection) =
            svc.AddSingleton<IDbConnectionFactory, DbConnectionFactory>(fun _ ->
                DbConnectionFactory(connectionString))

        webHost args {
            use_ifnot FalcoExtensions.IsDevelopment HstsBuilderExtensions.UseHsts
            use_https
            use_static_files

            add_service dbConnectionService

            endpoints [
                get Endpoints.index EntryController.index

                all Endpoints.entryCreate [
                    GET, EntryController.create
                    POST, EntryController.save ]

                all Endpoints.entryEdit [
                    GET, EntryController.edit
                    POST, EntryController.save ]
            ]
        }
        0