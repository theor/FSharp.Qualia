(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"
#I "../../packages"
#r "System.Core.dll"
#r "System.dll"
#r "System.Drawing.dll"
#r "System.Numerics.dll"
#r "System.Windows.Forms.dll"
#r "Rx-Core/lib/net45/System.Reactive.Core.dll"
#r "Rx-Interfaces/lib/net45/System.Reactive.Interfaces.dll"
#r "Rx-Linq/lib/net45/System.Reactive.Linq.dll"
#r "Rx-PlatformServices/lib/net45/System.Reactive.PlatformServices.dll"
#r "FSharp.Qualia/FSharp.Qualia.dll"
#r "FSharp.Qualia.WPF/FSharp.Qualia.WPF.dll"

(**
FSharp.Qualia
======================

Qualia is a MVC-ish UI framework heavily inspired by [React+Flux](https://facebook.github.io/flux/) and [FSharp.Desktop.UI](http://fsprojects.github.io/FSharp.Desktop.UI/)
The goal is to provide a decoupled architecture using [Rx](http://rx.codeplex.com/) and idiomatic F#, based on an unidirectional event loop.

This project started as UI architecture research. FSharp.Desktop.UI seemed like a great solution, but still relied on WPF bindings and had no facility to handle collections. Still, it would not exist without FSharp.Desktop.UI - a big THANK YOU to [@dmitry-a-morozov](https://github.com/dmitry-a-morozov) and [@forki](https://github.com/forki).

Architecture overview
---------------------

![Qualia Architecture Overview](img/qualia_archi.png)

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      The FSharp.Qualia library can be <a href="https://nuget.org/packages/FSharp.Qualia">installed from NuGet</a>:
      <pre>PM> Install-Package FSharp.Qualia</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

Example
-------

A minimal Qualia app needs 5 types:

- an Event discriminated union
- a model, with reactive properties
- a view, usually linked to a WinForms or WPF window, listening to the model changes and send events
- a dispatcher, handling these events and transforming the model
- an event loop, wiring all previous types

The example is a simple winforms numeric up down. The final app will look like this: ![numericupdown](img/numericupdown.png)
The + and - buttons increment or decrement the value, which is displayed in a label and a textbox. Changin the value in the textbox will update the value if it can be parsed to an int.

### Event type

This is the core of Qualia - the type describing everything that happens in the app.

*)
open System
open System.Windows.Forms
open FSharp.Qualia

type MainEvents = 
    | Up // + Button clicked
    | Down // - Button clicked
    | Edit of string // textbox content changed - the string is the new content of the textbox

(**
### Model

Only one property: the value.
ReactiveProperty is basically a wrapper around Rx BehaviorSubject. It's an IObservable that will raise the last stored value on subscription. Its parameter is the initial value

*)

type MainModel() = 
    member val Value = new ReactiveProperty<int>(0)
    
(**
### View
The view is split in two parts: the form itself and the Qualia View.

#### Form
The actual Form backing the view. all exposed members will be used by the view.

This could be a WinForms designer type, or a WPF one using the awesome [FsXaml](https://github.com/fsprojects/FsXaml/). There is a [WPF sample of the NumericUpDown](/) in the repo.
*)

type MainForm() =
    inherit Form()

    let p = new TableLayoutPanel()
    let addAt c i j =
        p.Controls.Add c
        p.SetCellPosition(c, TableLayoutPanelCellPosition(i,j))
        
    member val ButtonUp = new Button(Text="+")
    member val ButtonDown = new Button(Text="-")
    member val TextBox = new TextBox()
    member val Label = new Label()

    override x.OnLoad(e) =
        x.Controls.Add(p)
        addAt x.ButtonUp 1 0
        addAt x.ButtonDown 1 1
        addAt x.TextBox 0 0
        addAt x.Label 0 1
 
(**
#### Qualia View
The actual Form backing the view. all exposed members will be used by the view.
Qualia Views are templated with three types : the event one, the visual element one and the model. 
*)
type MainView(mw : MainForm, m) = 
    inherit View<MainEvents, Form, MainModel>(mw, m)
(** EventStreams returns a list of IObservable<Event>.
Observable.mapTo maps an observable to a constant.
There's also a --> operator defined for conciseness  *)    
    override this.EventStreams = 
        [ mw.ButtonUp.Click |> Observable.mapTo Up // map first button clicked event
          mw.ButtonDown.Click |> Observable.mapTo Down // second one
          mw.TextBox.TextChanged |> Observable.map (fun e -> Edit mw.TextBox.Text) ] // map textchanged events
    
(**
SetBindings subscribe to property changes in the model and updates the ui accordingly.
In this case, when the model value changes, we set both the label and the textbox content
*)

    override this.SetBindings(m : MainModel) = 
        m.Value.Add(fun v -> mw.Label.Text <- (string v))
        m.Value.Add(fun v -> mw.TextBox.Text <- (string v))

(**
### Dispatcher
The dispatcher is a glorifed event handler. It reacts to events sent by the view by modifing the model.
Each event can be handled synchronously or asynchronously. The Dispatcher method returns a wrapped handler.
*)

type MainDispatcher() = 
    // up and down events just increment or decrement the model's value
    let up (m : MainModel) = m.Value.Value <- m.Value.Value + 1
    let down (m : MainModel) = m.Value.Value <- m.Value.Value - 1
    // edit tries to parse the given string
    let edit str (m : MainModel) = 
        match Int32.TryParse str with
        | true, i -> m.Value.Value <- i
        | false, _ -> ()
    
    // the dispatcher does not deal with the view - just need the event and model types
    interface IDispatcher<MainEvents, MainModel> with
        member this.InitModel _ = () // nothing to do here
        member this.Dispatcher = 
            function 
            | Up -> Sync up
            | Down -> Sync down
            | Edit str -> Sync(edit str)
(**
### Event Loop
Finally, we need to use these types and run the application:
*)

[<STAThread>]
[<EntryPoint>]
let main argv = 
    let v = MainView(new MainForm(), MainModel())
    let mvc = EventLoop(v, MainDispatcher())
    use eventloop = mvc.Start() // wires all events/streams/...
    v.Root.ShowDialog() |> ignore // v.Root is the backing visual element provided, here the MainForm instance.
    0


(**
Some more info

Samples & documentation
-----------------------

The library comes with comprehensible documentation. 
It can include tutorials automatically generated from `*.fsx` files in [the content folder][content]. 
The API reference is automatically generated from Markdown comments in the library implementation.

 * [Tutorial](tutorial.html) contains a further explanation of this sample library.

 * [API Reference](reference/index.html) contains automatically generated documentation for all types, modules
   and functions in the library. This includes additional brief samples on using most of the
   functions.
 
Contributing and copyright
--------------------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork 
the project and submit pull requests. If you're adding a new public API, please also 
consider adding [samples][content] that can be turned into a documentation. You might
also want to read the [library design notes][readme] to understand how it works.

The library is available under Public Domain license, which allows modification and 
redistribution for both commercial and non-commercial purposes. For more information see the 
[License file][license] in the GitHub repository. 

  [content]: https://github.com/fsprojects/FSharp.Qualia/tree/master/docs/content
  [gh]: https://github.com/fsprojects/FSharp.Qualia
  [issues]: https://github.com/fsprojects/FSharp.Qualia/issues
  [readme]: https://github.com/fsprojects/FSharp.Qualia/blob/master/README.md
  [license]: https://github.com/fsprojects/FSharp.Qualia/blob/master/LICENSE.txt
*)
