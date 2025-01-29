namespace Falco.Security

module Xsrf =
    open System.Threading.Tasks
    open Falco.Markup
    open Microsoft.AspNetCore.Antiforgery
    open Microsoft.AspNetCore.Http
    open Microsoft.Extensions.DependencyInjection

    /// Outputs an antiforgery <input type="hidden" />.
    let antiforgeryInput
        (token : AntiforgeryTokenSet) =
        Elem.input [
            Attr.type' "hidden"
            Attr.name token.FormFieldName
            Attr.value token.RequestToken ]

    /// Generates a CSRF token using the Microsoft.AspNetCore.Antiforgery
    /// package.
    let getToken (ctx : HttpContext) : AntiforgeryTokenSet =
        let antiFrg = ctx.RequestServices.GetRequiredService<IAntiforgery>()
        antiFrg.GetAndStoreTokens ctx

    /// Validates the Antiforgery token within the provided HttpContext.
    let validateToken (ctx : HttpContext) : Task<bool> =
        let antiFrg = ctx.RequestServices.GetRequiredService<IAntiforgery>()
        antiFrg.IsRequestValidAsync ctx
