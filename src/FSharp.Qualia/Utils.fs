namespace FSharp.Qualia

open System
open System.Collections.Specialized

[<RequireQualifiedAccess>]
module Observable = 
    /// Convenience function to map an observable to a constant value
    let mapTo value = Observable.map (fun _ -> value)

[<RequireQualifiedAccess>]
module internal Observer = 
    open System.Reactive
    /// Synchronize the observer to limit reentrancy
    let preventReentrancy observer = Observer.Synchronize(observer, preventReentrancy = true)

[<AutoOpen>]
module Utils =
    /// printf-ish Trace.Writeline
    let tracefn format = Printf.kprintf (System.Diagnostics.Trace.WriteLine) format
    
    let traceidf (s) (x : 'a) = 
        tracefn s x
        x
    /// trace a value, then return it
    let traceid (x : 'a) = 
        tracefn "%A" x
        x

    /// Convenience operator for Observable.mapTo
    let inline (-->) (o:IObservable<_>) (value) = o |> Observable.mapTo value

    /// Returns true if the NotifyCollectionChangedEventArgs represents an addition or a removal
    let isAddOrRemove (x:NotifyCollectionChangedEventArgs) =
        x.Action = NotifyCollectionChangedAction.Add || x.Action = NotifyCollectionChangedAction.Remove

    /// Discriminated union to represent additions or removals to a list
    type CollectionChanged<'a> = Add of 'a seq | Remove of 'a seq

    /// Converts NotifyCollectionChangedEventArgs to CollectionChanged<'a>
    let toAddOrRemove<'a> (x:NotifyCollectionChangedEventArgs) =
        match x.Action with
        | NotifyCollectionChangedAction.Add -> Add (x.NewItems |> Seq.cast<'a>) |> Some
        | NotifyCollectionChangedAction.Remove -> Remove (x.OldItems |> Seq.cast<'a>) |> Some
        | _ -> None