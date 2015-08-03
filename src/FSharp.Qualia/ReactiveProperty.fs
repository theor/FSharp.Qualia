namespace FSharp.Qualia

open System.Reactive.Subjects
open System

/// Observable property holding the last value set. Setting the value will trigger an event in the embedded observable
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

[<AutoOpen>]
module ObservableExtensions = 
    /// creates a ReactiveProperty from an observable, with an initial value
    let toProperty (init:'a) (source:IObservable<'a>) =
        new ReactiveProperty<'a>(source, init)