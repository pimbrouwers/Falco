namespace AuthExample

open Falco

module AuthConfig =
    let authority = "https://falco-auth-test.us.auth0.com/"
    let audience = "https://users/api"

    let createUsersPolicy = "create:users"
    let readUsersPolicy = "read:users"
    let updateUsersPolicy = "update:users"
    let deleteUsersPolicy = "delete:users"

module Auth =
    let hasScope (scope : string) (next : HttpHandler) : HttpHandler =
        Request.ifAuthenticatedWithScope AuthConfig.authority scope next ErrorPages.forbidden