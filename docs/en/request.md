# Request Handling

The `HttpHandler` type is used to represent the processing of a request. It can be thought of as the eventual (i.e. asynchronous) completion and processing of an HTTP request, defined in F# as: `HttpContext -> Task`. Handlers will typically involve some combination of: route inspection, form/query binding, business logic and finally [response writing](/response). With access to the `HttpContext` you are able to inspect all components of the request, and manipulate the response in any way you choose.

## Accessing Request Data

> Falco exposes a [uniform API](#model-binding) to obtain typed values from the various sources of request data. Note, the similarity in the various binders below.

Reflection-based approaches to binding at IO boundaries work well for simple use cases. But as the complexity of the input rises it becomes error-prone and often involves tedious workarounds. This is especially true for an expressive, algebraic type system like F#. As such, it is often advisable to take back control of this process from the runtime. An added bonus of doing this is that it all but eliminates the need for `[<CLIMutable>]` attributes.

We can make this simpler by creating a succinct API to obtain typed values from `IFormCollection`, `IQueryCollection`, `RouteValueDictionary`, `IHeaderCollection`, and `IRequestCookieCollection`. All of which are derivatives of `StringCollectionReader` which is an abstraction intended to make it easier to work with the string-based key/value collections.

### Route Binding

```fsharp
let mapRouteHandler : HttpHandler =
    let routeMap (r : RouteCollectionReader) =
        r.GetString "Name" "John Doe"

    Request.mapRoute routeMap Response.ofJson

let manualRouteHandler : HttpHandler = fun ctx ->
    let r : RouteCollectionReader = Request.getRoute ctx
    let name = r.GetString "Name" "John Doe"

    Response.ofJson name ctx
```

### Query Binding

```fsharp
type Person = { FirstName : string; LastName : string }

let mapQueryHandler : HttpHandler =
    let queryMap (q : QueryCollectionReader) =
        let first = q.GetString "FirstName" "John" // Get value or return default value
        let last = q.GetString "LastName" "Doe"
        { FirstName = first; LastName = last }

    Request.mapQuery queryMap Response.ofJson

let manualQueryHandler : HttpHandler = fun ctx ->
    let q : QueryCollectionReader = Request.getQuery ctx

    let person =
        { FirstName = q.GetString "FirstName" "John" // Get value or return default value
          LastName  = q.GetString "LastName" "Doe" }

    Response.ofJson person ctx
```

### Form Binding

The `FormCollectionReader` has full access to the `IFormFilesCollection` via the `_.Files` member.

> Note the addition of `Request.mapFormSecure`, which will automatically validate CSRF token for you.

```fsharp
type Person = { FirstName : string; LastName : string }

let mapFormHandler : HttpHandler =
    let formMap (f : FormCollectionReader) =
        let first = f.GetString "FirstName" "John" // Get value or return default value
        let last = f.GetString "LastName" "Doe"
        { FirstName = first; LastName = last }

    Request.mapForm formMap Response.ofJson

let mapFormSecureHandler : HttpHandler =
    let formMap (f : FormCollectionReader) =
        let first = f.GetString "FirstName" "John" // Get value or return default value
        let last = f.GetString "LastName" "Doe"
        { FirstName = first; LastName = last }

    let handleInvalidCsrf : HttpHandler =
        Response.withStatusCode 400 >> Response.ofEmpty

    Request.mapFormSecure formMap Response.ofJson handleInvalidCsrf

let manualFormHandler : HttpHandler = fun ctx -> task {
    let! f : FormCollectionReader = Request.getForm ctx

    let person =
        { FirstName = f.GetString "FirstName" "John" // Get value or return default value
          LastName = f.GetString "LastName" "Doe" }

    return! Response.ofJson person ctx
}
```

#### `multipart/form-data` Binding

Microsoft defines [large uploads][15] as anything **> 64KB**, which well... is most uploads. Anything beyond this size and they recommend streaming the multipart data to avoid excess memory consumption.

To make this process **a lot** easier Falco provides a set of four `HttpHandler`'s analogous to the form handlers above, which utilize an `HttpContext` extension method called `TryStreamFormAsync()` that will attempt to stream multipart form data, or return an error message indicating the likely problem.

Below is an example demonstrating the insecure map variant:

```fsharp
let imageUploadHandler : HttpHandler =
    let formBinder (f : FormCollectionReader) : IFormFile option =
        f.TryGetFormFile "profile_image"

    let uploadImage (profileImage : IFormFile option) : HttpHandler =
        // Process the uploaded file ...

    // Safely buffer the multipart form submission
    Request.mapFormStream formBinder uploadImage
```

## JSON

Included in Falco are basic JSON in/out handlers, `Request.mapJson` and `Response.ofJson` respectively. Both rely on `System.Text.Json` and thus have minimal support for F#'s algebraic types.

```fsharp
type Person = { FirstName : string; LastName : string }

let jsonHandler : HttpHandler =
    { FirstName = "John"; LastName = "Doe" }
    |> Response.ofJson

let mapJsonHandler : HttpHandler =
    let handleOk person : HttpHandler =
        let message = sprintf "hello %s %s" person.First person.Last
        Response.ofPlainText message

    Request.mapJson handleOk
```