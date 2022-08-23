namespace Elmish.LiveSubs

[<AutoOpen>]
module Types =

    // Effect feels less weird than Sub in this context
    type Effect<'msg> = Elmish.Sub<'msg>

    /// Stops a live subscription.
    type StopLiveSub = unit -> unit

    /// Starts a live subscription when given a dispatch fn.
    /// Returns a stop function.
    type StartLiveSub<'msg> = Elmish.Dispatch<'msg> -> StopLiveSub

    /// SubId is a string for broadest compatibility.
    type SubId = string

    /// <summary>
    /// A live sub is a key-value of an id and a start function.
    /// The start function must return the subscription's stop function.
    ///
    /// example:
    ///
    /// <code>
    /// let subscribe model =
    ///     [ if model.Mode = On then
    ///         "app/clock", Time.every 1000 (Time.getNow Ticked) ]
    /// </code>
    /// </summary>
    type LiveSub<'msg> = SubId * StartLiveSub<'msg>


// type fns

module LiveSub =

    /// Change the msg type used by the subscription.
    let map (f: 'a -> 'msg) ((subId, startFn): LiveSub<'a>) : LiveSub<'msg> =
        subId, fun dispatch -> startFn (f >> dispatch)


// experimenting
module SubId =

    module Service =

        let [<Literal>] Prefix = "livesub/"

        type Model = int
        type Msg<'msg> = SubId -> 'msg

        let init () =
            1, []

        let update msg model =
            let subId = Prefix + string model
            model + 1, [fun dispatch -> dispatch (msg subId)]
