module Defs

open FsXaml
open System
open System.Reactive.Linq
open System.Windows
open System.Reactive
open System.Reactive.Subjects
open System.Windows.Controls
open System.Collections.ObjectModel
open System.Collections.Specialized
open System.Collections.Generic

let tracefn format = Printf.kprintf (System.Diagnostics.Trace.WriteLine) format

let traceid (x : 'a) = 
    tracefn "%A" x
    x

type DerivedCollection<'a, 'b when 'a : equality and 'b : equality>(src:ObservableCollection<'a>, f:'a->'b) as this =
    inherit ObservableCollection<'b>(Seq.map f src)
    let map = Dictionary()
    let collChanged (e:NotifyCollectionChangedEventArgs) =
        match e.Action with
        | NotifyCollectionChangedAction.Add ->
            let s = e.NewItems |> Seq.cast<'a>
            let ds = s |> Seq.map f
            Seq.iter2 (fun o d -> map.Add(o,d); this.Add d) s ds
        | NotifyCollectionChangedAction.Remove ->
            let s = e.OldItems |> Seq.cast<'a>
            let mapped = s |> Seq.map (map.TryGetValue) |> Seq.filter fst
            mapped |> Seq.map snd |> Seq.iter (this.Remove >> ignore)
//        | NotifyCollectionChangedAction.Replace -> ()
//        | NotifyCollectionChangedAction.Move -> ()
//        | NotifyCollectionChangedAction.Reset -> ()
        | _ -> failwith "Not Implemented"
    do
        Seq.iter2 (fun a b -> map.Add(a,b)) src this
        src.CollectionChanged.Add collChanged
        

let isAddOrRemove (x:NotifyCollectionChangedEventArgs) =
    x.Action = NotifyCollectionChangedAction.Add || x.Action = NotifyCollectionChangedAction.Remove
type CollectionChanged<'a> = Add of 'a seq | Remove of 'a seq
let toAddOrRemove<'a> (x:NotifyCollectionChangedEventArgs) =
    match x.Action with
    | NotifyCollectionChangedAction.Add -> Add (x.NewItems |> Seq.cast<'a>) |> Some
    | NotifyCollectionChangedAction.Remove -> Remove (x.OldItems |> Seq.cast<'a>) |> Some
    | _ -> None
type ReactiveProperty<'a>(init:'a) =
    let mutable value = init
    do
        tracefn "NEW PROP %A" typedefof<'a>
    member val private sub = new BehaviorSubject<'a>(init)
    interface IObservable<'a> with
        member x.Subscribe(observer: IObserver<'a>): IDisposable = 
            x.sub.Subscribe observer
        
    member x.Value
        with get() = value
        and set(v) = value <- v; x.sub.OnNext v

    override x.ToString() = sprintf "%A" x.Value


    new(source:IObservable<'a>, init:'a) as x =
        ReactiveProperty(init)
        then
            source |> Observable.map (traceid)
                   |> Observable.add (fun v -> x.Value <- v)
        
let Prop (init:'a) = new ReactiveProperty<'a>(init)

[<RequireQualifiedAccess>]
module internal Observer = 
    open System.Reactive
    open System.Windows.Threading
    
    let notifyOnDispatcher (observer : IObserver<_>) = 
        let dispatcher = Dispatcher.CurrentDispatcher
        
        let invokeOnDispatcher f = 
            if dispatcher.CheckAccess() then f()
            else dispatcher.InvokeAsync f |> ignore
        { new IObserver<_> with
              member __.OnNext value = invokeOnDispatcher (fun () -> observer.OnNext value)
              member __.OnError error = invokeOnDispatcher (fun () -> observer.OnError error)
              member __.OnCompleted() = invokeOnDispatcher observer.OnCompleted }
    
    let preventReentrancy observer = Observer.Synchronize(observer, preventReentrancy = true)

[<RequireQualifiedAccess>]
module Observable = 
    let mapTo value = Observable.map (fun _ -> value)
    let toProperty (init:'a) (source:IObservable<'a>) =
        new ReactiveProperty<'a>(source, init)

let inline (-->) (o:IObservable<_>) (value) = o |> Observable.mapTo value

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
type View<'Event, 'Element, 'Model when 'Element :> FrameworkElement>(elt : 'Element, m : 'Model) = 
    inherit IViewWithModel<'Event, 'Model>(m)
    member this.Root = elt

module CollectionSourceView =
    open System.Windows.Data

    type ViewConverter<'Event, 'Model, 'View, 'Element
        when 'View :> View<'Event, 'Element, 'Model>
        and 'Element :> FrameworkElement>
            (ff:('View -> 'View), f: 'Model -> 'View) =
        interface System.Windows.Data.IValueConverter with
            member x.Convert(value: obj, targetType: Type, parameter: obj, culture: Globalization.CultureInfo): obj = 
                let v = f (value :?> 'Model)
                let v2 = ff v
                v2.Root :> obj       

            member x.ConvertBack(value: obj, targetType: Type, parameter: obj, culture: Globalization.CultureInfo): obj = 
                failwith "Not implemented yet"

    [<AbstractClass>]
    type T<'Event, 'Element, 'Model when 'Element :> FrameworkElement>(elt : 'Element, m : 'Model) = 
        inherit View<'Event, 'Element, 'Model>(elt, m)
        member this.linkCollection (itemsControl : ItemsControl)
                                   (creator : 'ItemModel -> 'ItemView) 
                                   (coll : ObservableCollection<_>) =
            let it = DataTemplate(typedefof<'Model>)
            let fef = FrameworkElementFactory(typedefof<ContentPresenter>)
            let b = Binding(".")
            let conv =  ViewConverter(this.ComposeView, creator)
            b.Converter <- conv
            fef.SetBinding(ContentPresenter.ContentProperty, b)

            it.VisualTree <- fef

            itemsControl.ItemTemplate <- it
            let collView = CollectionViewSource.GetDefaultView(coll)
            itemsControl.ItemsSource <- collView
            collView

module DerivedCollectionSourceView =
    open System.Windows.Data

    [<AbstractClass>]
    type T<'Event, 'Element, 'Model when 'Element :> FrameworkElement>(elt : 'Element, m : 'Model) = 
        inherit View<'Event, 'Element, 'Model>(elt, m)
        member this.linkCollection (itemsControl : ItemsControl)
                                   (creator : 'ItemModel -> 'ItemView) 
                                   (coll : ObservableCollection<_>) =
            let it = DataTemplate(typedefof<'Model>)
            let fef = FrameworkElementFactory(typedefof<ContentPresenter>)
            let b = Binding("Root")
            fef.SetBinding(ContentPresenter.ContentProperty, b)

            it.VisualTree <- fef

            itemsControl.ItemTemplate <- it

            let derived = DerivedCollection(coll, creator >> this.ComposeView)
            let collView = CollectionViewSource.GetDefaultView(derived)
            itemsControl.ItemsSource <- collView
            collView

type EventHandler<'Model> = 
    | Sync of ('Model -> unit)
    | Async of ('Model -> Async<unit>)

type IController<'Event, 'Model> = 
    abstract InitModel : 'Model -> unit
    abstract Dispatcher : ('Event -> EventHandler<'Model>) with (*error(why, event)*) get

type MVC<'Model, 'Event, 'Element when 'Element :> Window>(v : View<'Event, 'Element, 'Model>, c : IController<'Event, 'Model>) = 
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
        |> Observer.notifyOnDispatcher
        |> hub.Subscribe
