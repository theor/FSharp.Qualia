namespace FSharp.Qualia.WPF

open System.Collections.ObjectModel
open System.Windows
open System.Windows.Controls
open FSharp.Qualia
open System.Windows.Data

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