namespace Elmish.LiveSubs

// FIXME HMR is detected after reload. Should detect before.

module Program =

    open Elmish

    [<AutoOpen>]
    module Types =

        type SubMsg<'msg> =
            | Stopped of SubId
            | Started of SubId * stop: StopLiveSub
            | UserMsg of 'msg
            #if FABLE_COMPILER && DEBUG
            | HmrInit
            #endif

        type SubModel<'model> =
            {
                Subs: Map<SubId, StopLiveSub>
                UserModel: 'model
            }


    module Fx =

        let start (subId, startFn) =
            fun dispatch ->
                let stop = startFn (UserMsg >> dispatch)
                dispatch (Started (subId, stop))

        let stop (subId, stop) =
            fun dispatch ->
                stop ()
                dispatch (Stopped subId)


    module Subs =

        let calcFx curKeys subStates newKeys liveSubs =
            let stopFx =
                subStates // set(current keys) - set(new keys) = stop keys
                |> Map.filter (fun k _ -> not (Set.contains k newKeys))
                |> Map.toList
                |> List.map Fx.stop
            let startFx =
                liveSubs // set(new keys) - set(current keys) = start keys
                |> List.filter (fun (k, _) -> not (Set.contains k curKeys))
                |> List.map Fx.start
            #if DEBUG
            printfn "updated subs: %A" (Set.toList newKeys)
            #endif
            List.concat [stopFx; startFx]

        let calcFxIfChanged subStates liveSubs =
            let curKeys = subStates |> Map.keys |> Set.ofSeq
            let newKeys = liveSubs |> List.map fst |> Set.ofList
            if curKeys = newKeys then
                []
            else
                calcFx curKeys subStates newKeys liveSubs

        let update subscribe subs (userModel, userCmd) =
            let liveSubs = subscribe userModel
            let subCmd = calcFxIfChanged subs liveSubs
            { Subs = subs; UserModel = userModel},
            Cmd.batch [subCmd; Cmd.map UserMsg userCmd]

        let updateIfChanged subscribe model (userModel, userCmd) =
            if model.UserModel = userModel then
                model, Cmd.map UserMsg userCmd
            else
                update subscribe model.Subs (userModel, userCmd)


    let withLiveSubs
        (subscribe : 'model -> LiveSub<'msg> list)
        (program : Program<'arg, 'model, 'msg, 'view>)
        : Program<'arg, SubModel<'model>, SubMsg<'msg>, 'view> =

        let mapInit userInit arg =
            userInit arg
            |> Subs.update subscribe Map.empty

        let mapUpdate userUpdate msg model =
            match msg with
            | Stopped subId ->
                {model with
                    Subs = Map.remove subId model.Subs}, []
            | Started (subId, subState) ->
                {model with
                    Subs = Map.add subId subState model.Subs}, []
            | UserMsg userMsg ->
                userUpdate userMsg model.UserModel
                |> Subs.updateIfChanged subscribe model
            #if FABLE_COMPILER && DEBUG
            // on hot reload, restart subs
            | HmrInit ->
                if Map.isEmpty model.Subs then
                    model, []
                else
                    printfn "[LiveSubs] hot reload detected, restarting subs"
                    let stopFx = Subs.calcFxIfChanged model.Subs []
                    Subs.update subscribe Map.empty (model.UserModel, [])
                    |> fun (model, startFx) -> model, List.append stopFx startFx
            #endif

        let mapView userView model dispatch =
            userView model.UserModel (UserMsg >> dispatch)

        let mapSetState userSetState model dispatch =
            userSetState model.UserModel (UserMsg >> dispatch)

        let mapSubscribe userSubscribe model =
            userSubscribe model.UserModel |> Cmd.map UserMsg
            #if FABLE_COMPILER && DEBUG
            // Elmish skips init but calls subscribe after hot reload
            |> fun cmd -> (fun dispatch -> dispatch HmrInit) :: cmd
            #endif

        program
        |> Program.map mapInit mapUpdate mapView mapSetState mapSubscribe

