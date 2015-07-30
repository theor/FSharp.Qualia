[<AutoOpen>]
module Utils

open System

[<RequireQualifiedAccess>]
module Observable = 
    let mapTo value = Observable.map (fun _ -> value)

[<RequireQualifiedAccess>]
module internal Observer = 
    open System.Reactive
    let preventReentrancy observer = Observer.Synchronize(observer, preventReentrancy = true)


let tracefn format = Printf.kprintf (System.Diagnostics.Trace.WriteLine) format

let traceid (x : 'a) = 
    tracefn "%A" x
    x

let inline (-->) (o:IObservable<_>) (value) = o |> Observable.mapTo value

