namespace Todo.Common

/// Internal URLs
module Urls = 
    let ``/`` = "/"
    let ``/todo/create`` = "/todo/create"
    let ``/todo/complete/{id}`` = sprintf "/todo/complete/%s"
    let ``/todo/incomplete/{id}`` = sprintf "/todo/incomplete/%s"

