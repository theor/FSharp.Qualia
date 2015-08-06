namespace FSharp.Qualia

open System.Collections.ObjectModel
open System.Collections.Generic
open System.Collections.Specialized

/// Collection based on a source observable collection, with a mapping function.
/// Each modification to the source collection will be reflected on the derived one.
/// Replace, Move and Reset events are not handled correctly at the time.
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
        | NotifyCollectionChangedAction.Replace -> ()
        | NotifyCollectionChangedAction.Move -> ()
        | NotifyCollectionChangedAction.Reset -> map.Clear(); this.Clear()
        | _ -> failwith "Not Implemented"
    do
        Seq.iter2 (fun a b -> map.Add(a,b)) src this
        src.CollectionChanged.Add collChanged