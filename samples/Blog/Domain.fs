module Blog.Domain

open System

type Post =
    {
        Slug  : string
        Title : string
        Date  : DateTime
        Body  : string
    }

