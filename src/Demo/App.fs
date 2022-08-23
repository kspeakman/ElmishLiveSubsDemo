module App

open Elmish.LiveSubs
open System


module Time =

    open Browser.Dom

    let getNow (tag: DateTime -> 'msg) : Effect<'msg> =
        fun dispatch -> dispatch (tag DateTime.Now)

    let every (intervalMs: int) (effect: Effect<'msg>) : StartLiveSub<'msg> =
        let start dispatch : StopLiveSub =
            let handler () = effect dispatch
            let intervalId = window.setInterval(handler, intervalMs)
            let stop () = // hooray for closures
                window.clearInterval(intervalId)
            stop
        start


module Types =

    type Mode = Off | Starting | On

    type Model =
        { Mode: Mode
          Hour: int
          Minute: int
          Second: int
          Interval: int
          Transitions: bool }

    type Msg =
        | ChangeInterval of mark: string
        | Ticked of DateTime
        | Start
        | Stop


module Model =

    open Types

    let init () =
        { Mode = Starting
          Hour = 0
          Minute = 0
          Second = 0
          Interval = 1000
          Transitions = false },
        [Time.getNow Ticked]

    // TODO handle roll-over backward-spin behavior
    let update msg model =
        let toTime (d: DateTime) =
            int (Math.Round(float (d.Millisecond / 1000)))
            + d.Second + (d.Minute * 60) + (d.Hour * 3600)
        match msg with
        | ChangeInterval mark ->
            {model with Interval = int mark * 1000}, []
        | Start ->
            {model with Mode = Starting}, [Time.getNow Ticked]
        | Stop ->
            {model with Mode = Off; Transitions = false}, []
        | Ticked d ->
            let time = toTime d
            { model with
                Mode =
                    match model.Mode with
                    | Starting -> On
                    | _ -> model.Mode
                Hour = time / 120 //<sec/degree>
                Minute = time / 10 //<sec/degree>0
                Second = time * 6 //<degree/sec>
                // intentionally using prev Mode
                Transitions = //model.Mode = On }, // bug: always false after hot reload
                    match model.Mode with
                    | On -> true
                    | _ -> false },
            []

    let subscribe model =
        let subId = "app/clock/interval/" + string model.Interval
        match model.Mode with
        | Off | Starting -> []
        | On -> [subId, Time.every model.Interval (Time.getNow Ticked)]


module View =

    open Fable.React
    open Fable.React.Props
    open Fable.Core.JsInterop
    open Types // to shadow HTMLAttr.Start

    // TODO move to lib
    let inline ClassList (classList: string list) : HTMLAttr =
        let classes =
            classList |> List.filter (not << String.IsNullOrWhiteSpace)
        !!("className", String.Join(" ", classes))


    let toStyle degree =
        Transform (sprintf "rotateZ(%ideg)" degree)


    let view model dispatch =
        let transitions = if model.Transitions then "" else "no-transitions"
        let clock =
            div [ClassList ["clock"; transitions]] [
                div [Class "marks"] [
                    div [Class "mark r0"] []
                    div [Class "mark r30"] []
                    div [Class "mark r60"] []
                    div [Class "mark r90"] []
                    div [Class "mark r120"] []
                    div [Class "mark r150"] []
                ]
                div [Class "label"] []
                div [Class "hands"] [
                    div [Class "hand hours"; Style [toStyle model.Hour]] []
                    div [Class "hand minutes"; Style [toStyle model.Minute]] []
                    div [Class "hand seconds"; Style [toStyle model.Second]] []
                ]
                div [Class "hand-pin"] []
            ]
        let msg, text, classes =
            match model.Mode with
            | Off ->
                Start, "OFF", "fa-flip-horizontal has-text-grey-light"
            | Starting | On ->
                Stop, "ON", "has-text-info"
        div [Class "container is-block is-max-desktop p-1"] [
            section [Class "section"] [
                div [Class "columns"] [
                    div [Class "column"] [
                        h1 [Class "title"] [ str "Demo" ]
                        h2 [Class "subtitle"] [ str "Elmish live subscriptions" ]
                    ]
                    div [Class "column is-flex is-align-items-center is-justify-content-flex-end"] [
                        a [Href "https://github.com/elmish/elmish/issues/183"; Target "_blank"; Class "has-text-centered"] [
                            span [Class "icon"] [i [Class "far fa-circle-dot fa-xl"] []]
                            br []
                            span [] [str "elmish/183"]
                        ]
                        a [Href "https://github.com/kspeakman/ElmishLiveSubsDemo"; Target "_blank"; Class "has-text-centered ml-5"] [
                            span [Class "icon"] [i [Class "fab fa-github fa-xl"] []]
                            br []
                            span [] [str "source"]
                        ]
                    ]
                ]
            ]
            div [Class "box p-0 is-clipped"] [
                div [Class "columns m-0"] [
                    div [Class "column is-4 p-5"] [
                        div [Class "level"] [
                            div [Class "level-left"] [
                                div [Class "level-item"] [
                                    h1 [Class "title is-6"] [str "Clock"]
                                ]
                            ]
                            div [Class "level-right"; OnClick (fun _ -> dispatch msg)] [
                                div [Class "level-item"] [
                                    b [Class "is-size-7"] [str text]
                                ]
                                div [Class "level-item"] [
                                    i [ClassList ["fas fa-toggle-on fa-2xl"; classes]] []
                                ]
                            ]
                        ]
                        div [Class "level"] [
                            div [Class "level-item"] [str "Interval"]
                            div [Class "level-item"] [
                                input [ Type "range"
                                        DefaultValue (string (model.Interval / 1000))
                                        List "intervals"
                                        Min 1; Max 5; Step 1
                                        OnInput (fun e -> dispatch (ChangeInterval e.Value))]
                                datalist [Id "intervals"] [
                                    option [Value "1"] []
                                    option [Value "2"] []
                                    option [Value "3"] []
                                    option [Value "4"] []
                                    option [Value "5"] []
                                ]
                            ]
                            div [Class "level-item"] [str (string model.Interval + "ms")]
                        ]
                        br []
                        h6 [Class "title is-6"] [str "Model"]
                        pre [] [str (sprintf "%A" model)]
                        br []
                        br []
                        h6 [Class "title is-6"] [str "Subscriptions"]
                        let subIds = Model.subscribe model |> List.map fst
                        pre [] [str (sprintf "%A" subIds)]
                    ]
                    div [Class "column is-8 m-0 p-0 clock-bg"] [
                        div [Id "clock"] [ clock ]
                    ]
                ]
            ]
        ]


let init = Model.init
let update = Model.update
let subscribe = Model.subscribe
let view = View.view


// TMYK Our base-60 time system originated around 3000 BCE in ancient Sumeria.[1]
//      60 divides evenly by many numbers, making it a practical choice for everyday use.
//      Decimal fractions were discovered much later in the 9th century CE by Muḥammad ibn Mūsā al-Khwārizmī.[2]
//      The word "algorithm" comes from Al-Khwārizmī's name translated into Latin.
//      [1] https://en.wikipedia.org/wiki/Sexagesimal
//      [2] https://en.wikipedia.org/wiki/Muhammad_ibn_Musa_al-Khwarizmi
