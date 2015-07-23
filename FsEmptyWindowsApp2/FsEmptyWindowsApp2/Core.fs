module Defs

open FsXaml
open System
open System.Reactive.Linq
open System.Windows
open System.Reactive
open System.Reactive.Subjects
    
let tracefn format = Printf.kprintf (System.Diagnostics.Trace.WriteLine) format
let traceid (x:'a) = tracefn "%A" x; x

[<RequireQualifiedAccess>]
module internal Observer =

    open System.Reactive
    open System.Windows.Threading

    let notifyOnDispatcher(observer : IObserver<_>) = 
        let dispatcher = Dispatcher.CurrentDispatcher 
        let invokeOnDispatcher f = if dispatcher.CheckAccess() then f() else dispatcher.InvokeAsync f |> ignore 
        { 
            new IObserver<_> with 
                member __.OnNext value = invokeOnDispatcher(fun() -> observer.OnNext value)
                member __.OnError error = invokeOnDispatcher(fun() -> observer.OnError error)
                member __.OnCompleted() = invokeOnDispatcher observer.OnCompleted
        }    

    let preventReentrancy observer = Observer.Synchronize(observer, preventReentrancy = true)
[<RequireQualifiedAccess>]
module Observable =
    let mapTo value = Observable.map(fun _ -> value) 
    
[<AbstractClass>]
type IView<'Event>() =
    member val composeViewEvent = Event<IView<'Event>>()
    member this.ComposeView<'SubView,'SubModel when 'SubView :> IViewWithModel<'Event, 'SubModel>>(v:'SubView): 'SubView =
        v.SetBindings v.Model
        this.composeViewEvent.Trigger v; v
    abstract EventStreams : IObservable<'Event> list
    member this.Events = this.EventStreams.Merge()
and 
    [<AbstractClass>]
    IViewWithModel<'Event, 'Model>(m:'Model) =
    inherit IView<'Event>()
    member val Model:'Model = m
    abstract SetBindings : 'Model -> unit
    
[<AbstractClass>]
type View<'Event, 'Element, 'Model when 'Element :> FrameworkElement>(elt:'Element, m:'Model) =
    inherit IViewWithModel<'Event, 'Model>(m)
    member this.Root = elt


type EventHandler<'Model> = 
    | Sync of ('Model -> unit)
    | Async of ('Model -> Async<unit>)

type IController<'Event, 'Model> =
    abstract InitModel : 'Model -> unit
    abstract Dispatcher : ('Event -> EventHandler<'Model>)

type MVC<'Model,'Event,'Element when 'Element :> Window>(v:View<'Event, 'Element, 'Model>, c:IController<'Event,'Model>) =
    let hub = new Subject<'Event>()
    let error(why, event) = tracefn "%A %A" why event
    do
        let subscribe (e:IView<'Event>) = 
            tracefn "COMPOSE %A" e
            e.Events.Subscribe hub |> ignore
        v.composeViewEvent.Publish |> Observable.subscribe subscribe |> ignore
    member this.Create() = ()
    member this.Start() =
        c.InitModel v.Model
        v.SetBindings(v.Model)
        v.Events.Subscribe hub |> ignore
        Observer.Create(fun e ->
            match c.Dispatcher e with
            | Sync eventHandler ->
                try eventHandler v.Model
                with why -> error(why, e)
            | Async eventHandler -> 
                Async.StartWithContinuations(
                    computation = eventHandler v.Model, 
                    continuation = ignore, 
                    exceptionContinuation = (fun why -> () (*error(why, event)*)),
                    cancellationContinuation = ignore))
        |> Observer.preventReentrancy
        |> Observer.notifyOnDispatcher
        |> hub.Subscribe