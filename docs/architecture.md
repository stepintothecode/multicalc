# Architecture

Five projects, each with one job, arranged so the interesting logic runs without a phone.

```text
MultiCalc.Abstractions   the seams: interfaces and the values that cross them. No dependencies.
MultiCalc.Domain         calculation, sessions, history, settings. Pure .NET, no platform.
MultiCalc.Evaluation     arithmetic on top of NCalc. The only project that knows NCalc exists.
MultiCalc.Storage        JSON files for sessions and settings.
MultiCalc.Ui             Blazor components and the app state they share.
MultiCalc.App            MAUI host, Android adapters, composition root.
```

Dependencies point one way:

```text
Abstractions <- Domain <- Storage
     ^            ^
     |            |
Evaluation      Ui  <- App
```

`App` is the only project that references MAUI or Android. Everything below it is plain
.NET and is covered by `dotnet test` with no workload and no device.

## The calculation path

A key press never touches arithmetic directly:

```text
Keypad -> CalculatorState.Press -> ExpressionDraft.Press -> new draft
Equals -> CalculatorState.Evaluate -> CalculationService -> ICalculatorEngine
```

`ExpressionDraft` is an immutable value that holds what is typed and every rule about
which key may follow which. A key that would produce nonsense returns the draft unchanged
rather than a broken expression, so the engine never sees garbage from the keypad.

`CalculationService` sits between the draft and the engine and owns the two things the
engine has no business knowing: closing unclosed brackets, and what percent means on a
calculator. `PercentExpander` does the rewriting, which is why `50+10%` can stay on the
display as typed while the engine gets `50+((50)*(10)/100)`.

`NCalcCalculatorEngine` is the only place NCalc appears. It runs in decimal so `0.1+0.2`
comes to `0.3`, and it turns every failure into a named `EvaluationError` rather than
throwing.

## State

`CalculatorState` is a singleton holding a `SessionBook`. The book is an immutable value:
adding, closing, renaming and switching all return a new book, and the state swaps its
reference and raises `Changed`. Components subscribe and repaint.

Saving is debounced. Typing schedules a write 600 ms out and cancels any pending one, so
a burst of key presses is one write rather than twenty. Structural changes save at once,
and `FlushAsync` runs when the window stops, because Android kills processes without
warning.

## Seams

Everything the phone provides sits behind an interface in `Abstractions`, with a real
implementation in `App` and a fake in `tests/support`:

| Seam | Real | Fake |
| --- | --- | --- |
| `ICalculatorEngine` | `NCalcCalculatorEngine` | `StubCalculatorEngine` |
| `INativeCalculatorLauncher` | `AndroidNativeCalculatorLauncher` | `FakeNativeCalculatorLauncher` |
| `IFileExporter` | `MauiFileExporter` | `FakeFileExporter` |
| `IFileReader` | `MauiFileReader` | `FakeFileReader` |
| `IHaptics` | `AndroidHaptics` | `FakeHaptics` |
| `IExternalBrowser` | `MauiExternalBrowser` | none needed yet |
| `IClock` | `SystemClock` | `FakeClock` |
| `ISessionStore` | `JsonSessionStore` | `FakeSessionStore` |
| `ISettingsStore` | `JsonSettingsStore` | `FakeSettingsStore` |

That is what lets `CalculatorStateTests` cover both New buttons, the history import and
the device-cannot-spawn case on a build server with no phone attached.

`AndroidHaptics` talks to the system `Vibrator` rather than going through MAUI's
`HapticFeedback`, which stays silent on plenty of Android builds. It asks for the
predefined TICK effect where the phone has one, so a key press feels like the keyboard.

## Spawning a system calculator

`AndroidNativeCalculatorLauncher` is the one piece that cannot be unit tested, and it is
deliberately small.

Finding the calculator uses the category Android reserves for it rather than guessing at
package names, which differ on every manufacturer's build:

```text
Intent(ACTION_MAIN) + CATEGORY_APP_CALCULATOR -> queryIntentActivities
```

Android 11 and later hide other apps unless they are declared, so `<queries>` in
`AndroidManifest.xml` carries that same intent. Without it the probe finds nothing.

Spawning sets two flags on an explicit component:

```text
FLAG_ACTIVITY_NEW_TASK | FLAG_ACTIVITY_MULTIPLE_TASK   (0x18000000)
```

`NEW_TASK` puts the calculator in its own task; `MULTIPLE_TASK` stops Android reusing the
task that already exists. Together they give a separate window with its own Recents card
and its own state.

This only works when the target activity's launch mode is `standard` or `singleTop`.
`singleTask` and `singleInstance` collapse back into the existing task whatever flags are
set, so the launch mode is read during the probe and the option is hidden when it will
not work. That check is the difference between a button that does nothing and a UI that
tells the truth about the device.

The launch runs from `Platform.CurrentActivity` when there is one, which keeps it inside
Android's rules on background activity launches.

## Storage

Two JSON files in `FileSystem.AppDataDirectory`, written through `AtomicFile` (temp file
then move) so a process killed mid save does not leave a half written file.

Both documents carry a `Version`. A file from a newer build is refused rather than half
read, and an unreadable file means a fresh start rather than a crash on launch.

Serialisation is source generated. The Release build for Android trims unused metadata,
which breaks reflection based serialisation, and `HistoryExport` writes its JSON with
`Utf8JsonWriter` for the same reason.

## UI

Blazor Hybrid in a `BlazorWebView`. One route for the calculator and one each for
Settings, About and Support; everything else is a bottom sheet over the calculator, so
the keypad is never more than one tap away.

Theme is a `data-theme` attribute on the document root, pushed from `ThemeApplier` after
render. The stylesheet defines the light palette on bare `:root`, redefines it under
`prefers-color-scheme: dark` guarded against an explicit light choice, and again under
`[data-theme="dark"]`, so all three settings resolve correctly. `App.xaml.cs` sets the
matching native `UserAppTheme` so the status bar agrees with the page.
