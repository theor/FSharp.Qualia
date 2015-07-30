namespace FSharp.Qualia

open System.Reactive.Subjects
open System

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