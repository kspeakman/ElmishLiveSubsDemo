module Main

open Elmish
open Elmish.LiveSubs
open Elmish.React
open Fable.Core.JsInterop
open Elmish.HMR // shadows Program.<fn>

importAll "./styles.css"

let initArg = ()

Program.mkProgram App.init App.update App.view
|> Program.withLiveSubs App.subscribe
|> Program.withReactBatched "elmish-app"
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.runWith initArg
