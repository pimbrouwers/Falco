<!-- Hello Falco -->
<div class="cf mw9 center pv4 pv5-l ph3">

<!-- text -->
<div class="fl-l w-40-l mb4 mb0-l lh-copy">
<h2 class="mb3 mt0 pb1 f2 fw4 bb b--moon-gray">Hello, Falco!</h2>

<div class="mb3">Believe it or not, this is a <u>complete Falco application</u>. It does exactly what you'd expect. Upon compiling and running the program, if you visit <strong>https://localhost:5001/Falco</strong>, you would be greeted with:</div>

<div class="dib mb3 pa1 bg-merlot white br1">Hello, Falco!</div>

<div>If you visit without specifying a <code>{name}</code>, "world" will be substituted in place of the name. Resulting in "Hello, world". This simple, but illustrative program demonstrates Falco's modular design, in this case specifically request interaction and response writing.</div>
</div>
<!-- /end text -->

<!-- code -->
<div class="fl-l w-60-l pl4-l">

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder

let helloHandler : HttpHandler =       
    let routeBinder (r : RouteCollectionReader) =
        r.GetString "name" "world" |> sprintf "Hello, %s!"
    
    Request.mapRoute routeBinder Response.ofPlainText 

webHost [||] {
    endpoints [ get "/{name?}" helloHandler ]
}    
```

</div>
<!-- /end code --> 

</div>
<!-- /end Hello Falco>


<!-- HTML View Engine  -->
<div class="cf pv4 pv5-l bg-near-white">
<div class="mw9 center ph3">

<!-- text --> 
<div class="fr-l w-40-l mb4 mb0-l pl4-l lh-copy">

<h2 class="mb3 mt0 pb1 f2 fw4 bb b--moon-gray">Markup? Check!</h2>

<div class="mb3">A core feature of Falco is the XML markup module. It can be used to produce any form of angle-bracket markup (i.e. HTML, SVG, XML etc.), most notably HTML. Most of the standard HTML tags & attributes have been built into the markup module, which are pure functions composed into well-formed markup at run time. HTML tag functions are found in the <code>Elem</code> module, attributes in the <code>Attr</code> module. For string literal output there are several functions available in the <code>Text</code> module.</div>

<div class="mb3">The benefits of using the Falco markup module as an HTML engine include:</div>

<ul>
    <li>Writing your views in plain F#, directly in your assembly.</li>
    <li>Markup is compiled alongside the rest of your code, leading to improved performance and ultimately simpler deployments.</li>
</ul>

<div>Since views are plain F# they can easily be made strongly-typed, and more interestingly combined to create complex output.</div>

</div>
<!-- /end text -->

<!-- code -->
<div class="fl-l w-60-l">

```fsharp
open Falco
open Falco.Markup

type Person = { First : string; Last  : string }

let master (title : string) (content : XmlNode list) =
    Elem.html [ Attr.lang "en" ] [
        Elem.head [] [ Elem.title [] [ Text.raw title ] ]
        Elem.body [] content 
    ]

let divider = 
    Elem.hr [ Attr.class' "divider" ]

let personView (p : Person) =     
    master "Sample App" [                     
        Elem.main [] [   
            Elem.h1 [] [ Text.raw "Your name is:" ]
            divider
            Elem.p  [] [ Text.rawf "%s %s" p.First p.Last ] 
        ] 
    ]    
```

</div>
<!-- /end code -->

</div>
</div>
<!-- /end HTML View Engine -->


<!-- Model Binding -->
<div class="cf mw9 center pv4 pv5-l ph3">

<!-- text -->
<div class="fl-l w-40-l mb4 mb0-l lh-copy">
<h2 class="mb3 mt0 pb1 f2 fw4 bb b--moon-gray">Magic-free model binding.</h2>

<div class="mb3">Reflection-based approaches to binding at IO boundaries work well for simple use cases. But as the complexity of the input rises it becomes error-prone and often involves tedious workarounds. This is especially true for an expressive, algebraic type system like F#. As such, it is often advisable to take back control of this process from the runtime.</div>

<div>We can make this simpler by creating a succinct API to obtain typed values from <code>IFormCollection</code>, <code>IQueryCollection</code>, <code>RouteValueDictionary</code> and <code>IHeaderCollection</code>. <i>Readers</i> for all four exist as derivatives of <code>StringCollectionReader</code> which is an abstraction intended to make it easier to work with the string-based key/value collections.</div>

</div>
<!-- /end text -->

<!-- code -->
<div class="fl-l w-60-l pl4-l">

```fsharp
open Falco

type CardType = Visa | MasterCard | AmericanExpress
type CreditCard = { Type : CardType; Number : int; Cvd : int option }

let handleMapForm : HttpHandler = 
    let formMap (f : FormCollectionReader) =        
        let cardType = 
            match f.GetString "card_type" "visa" with
            | "MasterCard"      -> MasterCard
            | "AmericanExpress" -> AmericanExpress
            | _                 -> Visa
        
        let cardNum = f.GetInt32 "card_number" 0
        let cardCvd = f.TryGetInt32 "cvd"

        { Type = cardType; Number = cardNumber; Cvd = cardCvd }

    Request.mapForm formMap Response.ofJson
```

</div>
<!-- /end code --> 

</div>
<!-- /end Model Binding -->
