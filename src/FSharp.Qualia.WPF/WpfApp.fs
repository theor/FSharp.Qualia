namespace FSharp.Qualia

module WpfApp =
    open System.Windows
    open System.Threading

    let runApp<'Event, 'Element, 'Model, 'App
        when 'App :> Application
        and 'Element :> Window> (el:EventLoop<'Model, 'Event, 'Element>)
                                 (v:View<'Event, 'Element, 'Model>)
                                 (app:'App) =
        app.Startup |> Observable.add (fun _ ->
            let sc = SynchronizationContext.Current
            el.StartWithScheduler(fun f -> sc.Send((fun _ -> f()), null)) |> ignore)
        app.Run(v.Root)

