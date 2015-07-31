namespace FSharp.Qualia

open System
open System.Collections.Specialized

module Core =
    let isAddOrRemove (x:NotifyCollectionChangedEventArgs) =
        x.Action = NotifyCollectionChangedAction.Add || x.Action = NotifyCollectionChangedAction.Remove
    type CollectionChanged<'a> = Add of 'a seq | Remove of 'a seq
    let toAddOrRemove<'a> (x:NotifyCollectionChangedEventArgs) =
        match x.Action with
        | NotifyCollectionChangedAction.Add -> Add (x.NewItems |> Seq.cast<'a>) |> Some
        | NotifyCollectionChangedAction.Remove -> Remove (x.OldItems |> Seq.cast<'a>) |> Some
        | _ -> None
    
[<AutoOpen>]
module ObservableExtentions = 
    let toProperty (init:'a) (source:IObservable<'a>) =
        new ReactiveProperty<'a>(source, init)
