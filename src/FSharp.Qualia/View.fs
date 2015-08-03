namespace FSharp.Qualia

open System
open System.Collections.ObjectModel

[<AbstractClass>]
/// [omit]
/// Base View type, parameterized with the Event type
type IView<'Event>() = 
    /// event fired when a sub-view is composed
    member val composeViewEvent = Event<IView<'Event>>()
    
    /// Compose a subview in the current view
    member this.ComposeView<'SubView, 'SubModel when 'SubView :> IViewWithModel<'Event, 'SubModel>>(v : 'SubView) : 'SubView = 
        v.SetBindings v.Model
        this.composeViewEvent.Trigger v
        v
    
    /// List of event observable sources, subscribed to by the dispatcher
    abstract EventStreams : IObservable<'Event> list

and [<AbstractClass>]
    /// [omit]
    /// Intermediary View type, parameterized with both Event and Model types
    IViewWithModel<'Event, 'Model>(m : 'Model) = 
    inherit IView<'Event>()
    member val Model : 'Model = m
    /// Will subscribe to the model's changed, typically on its ReactiveProperties
    abstract SetBindings : 'Model -> unit

[<AbstractClass>]
/// 
type View<'Event, 'Element, 'Model>(elt : 'Element, m : 'Model) = 
    inherit IViewWithModel<'Event, 'Model>(m)
    /// The visual element
    member this.Root = elt

/// Derived Collections helpers
module CollectionsHelper =
    /// Will create a derived collection  for a view
    /// Each source item will be mapped to a view item in the derived collection, and this view is composed in the main view during addition
    let createDerivedCollection<'Event, 'ItemModel, 'ItemView
        when 'ItemModel : equality and 'ItemView : equality and 'ItemView :> IViewWithModel<'Event, 'ItemModel>>
            (view:IView<'Event>)
            (creator : 'ItemModel -> 'ItemView) 
            (coll : ObservableCollection<_>) =
        let derived = DerivedCollection(coll, creator >> view.ComposeView)
        derived