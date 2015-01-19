namespace SinglePageApplication1

open IntelliFactory.WebSharper
open IntelliFactory.WebSharper.JavaScript
open IntelliFactory.WebSharper.Html.Client

[<JavaScript>]
module Client =

    let Main =
        Console.Log("Running JavaScript Entry Point..")

        let welcome = P [Text "Welcome"]
        Div [
            welcome
            Button [Text "Click Me!"]
            |>! OnClick (fun e args ->
                welcome.Text <- "Hello, world!")
        ]


        
        |> fun el ->
          el.AppendTo "entrypoint"