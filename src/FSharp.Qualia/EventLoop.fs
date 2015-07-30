namespace FSharp.Qualia

open System.Reactive
open System.Reactive.Subjects

type EventHandler<'Model> = 
    | Sync of ('Model -> unit)
    | Async of ('Model -> Async<unit>)

type IDispatcher<'Event, 'Model> = 
    abstract InitModel : 'Model -> unit
    abstract Dispatcher : ('Event -> EventHandler<'Model>) with (*error(why, event)*) get

//type Dispatcher<'Event, 'Model> = {
//    InitModel : 'Model -> unit
//    Dispatcher : ('Event -> EventHandler<'Model>)
//}

type EventLoop<'Model, 'Event, 'Element>(v : View<'Event, 'Element, 'Model>, c : IDispatcher<'Event, 'Model>) = 
    let hub = new Subject<'Event>()
    let error (why, event) = tracefn "%A %A" why event
    
    do 
        let subscribe (e : IView<'Event>) = 
            tracefn "COMPOSE %A" e
            e.Events.Subscribe hub |> ignore
        v.composeViewEvent.Publish
        |> Observable.subscribe subscribe
        |> ignore
    
    member this.Create() = ()
    member this.Start() = 
        c.InitModel v.Model
        v.SetBindings(v.Model)
        v.Events.Subscribe hub |> ignore
        Observer.Create
            (fun e -> 
            match c.Dispatcher e with
            | Sync eventHandler -> 
                try 
                    eventHandler v.Model
                with why -> error (why, e)
            | Async eventHandler -> 
                Async.StartWithContinuations
                    (computation = eventHandler v.Model, continuation = ignore, exceptionContinuation = (fun why -> ()), 
                     cancellationContinuation = ignore))
        |> Observer.preventReentrancy
        |> hub.Subscribe
