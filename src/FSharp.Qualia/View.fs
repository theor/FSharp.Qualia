namespace FSharp.Qualia

open System
open System.Reactive.Linq

[<AbstractClass>]
type IView<'Event>() = 
    member val composeViewEvent = Event<IView<'Event>>()
    
    member this.ComposeView<'SubView, 'SubModel when 'SubView :> IViewWithModel<'Event, 'SubModel>>(v : 'SubView) : 'SubView = 
        v.SetBindings v.Model
        this.composeViewEvent.Trigger v
        v
    
    abstract EventStreams : IObservable<'Event> list
    member this.Events = this.EventStreams.Merge()

and [<AbstractClass>] IViewWithModel<'Event, 'Model>(m : 'Model) = 
    inherit IView<'Event>()
    member val Model : 'Model = m
    abstract SetBindings : 'Model -> unit

[<AbstractClass>]
type View<'Event, 'Element, 'Model>(elt : 'Element, m : 'Model) = 
    inherit IViewWithModel<'Event, 'Model>(m)
    member this.Root = elt