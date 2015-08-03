namespace FSharp.Qualia.WPF

open System.Collections.ObjectModel
open System.Windows
open System.Windows.Controls
open System.Windows.Data
open FSharp.Qualia

/// Helper for WPF Collections
module WPFCollectionsHelper =
    /// Links a derived collection to an items control. Creates a datatemplate for the given 'ItemView type, bound on the view's Root property.
    /// Returns the WPF ICollectionView  for filtering, grouping, ...
    let linkCollection<'Event, 'ItemModel, 'ItemView
        when 'ItemModel : equality and 'ItemView : equality and 'ItemView :> IViewWithModel<'Event, 'ItemModel>>
            (itemsControl : ItemsControl)
            (derived: DerivedCollection<'ItemModel,'ItemView>) =
        let it = DataTemplate(typedefof<'ItemModel>)
        let fef = FrameworkElementFactory(typedefof<ContentPresenter>)
        let b = Binding("Root")
        fef.SetBinding(ContentPresenter.ContentProperty, b)

        it.VisualTree <- fef

        itemsControl.ItemTemplate <- it

        let collView = CollectionViewSource.GetDefaultView(derived)
        itemsControl.ItemsSource <- collView
        collView

[<AbstractClass>]
/// WPF Collection view convenience class
type DerivedCollectionSourceView<'Event, 'Element, 'Model
    when 'Element :> FrameworkElement>(elt : 'Element, m : 'Model) = 

    inherit View<'Event, 'Element, 'Model>(elt, m)

    /// creates a derived collection and set it as the provided ItemsControl source.
    /// Returns the resulting WPF ICollectionView for filtering, grouping, ...
    member x.linkCollection<'ItemModel, 'ItemView
         when 'ItemModel : equality and 'ItemView : equality and 'ItemView :> IViewWithModel<'Event, 'ItemModel>>
        (itemsControl : ItemsControl)
        (creator : 'ItemModel -> 'ItemView) 
        (coll : ObservableCollection<_>) =
        let derived =  CollectionsHelper.createDerivedCollection x creator coll
        WPFCollectionsHelper.linkCollection itemsControl derived
    