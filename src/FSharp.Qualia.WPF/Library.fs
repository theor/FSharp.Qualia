namespace FSharp.Qualia.WPF

open System.Windows
open System.Windows.Controls
open System.Windows.Data
open FSharp.Qualia
open FSharp.Qualia.Defs
open System.Collections.ObjectModel

[<RequireQualifiedAccess>]
module internal Observer = 
    open System
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



[<AbstractClass>]
type DerivedCollectionSourceView<'Event, 'Element, 'Model when 'Element :> FrameworkElement>(elt : 'Element, m : 'Model) = 
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