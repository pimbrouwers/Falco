namespace Falco.Tests.Extensions

module Response =
    open NSubstitute
    open Microsoft.AspNetCore.Http

    type HttpResponse with
    member this.ReceivedTemporaryRedirectTo(url: string) =
        this.Received().Redirect(url, false)
    member this.ReceivedPermanentRedirectTo(url: string) =
        this.Received().Redirect(url, true)
