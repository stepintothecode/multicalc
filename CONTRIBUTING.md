# Contributing

## Setup

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- `dotnet workload install maui-android`
- A JDK 17 or later, and the Android SDK

```bash
dotnet build
dotnet test
dotnet build src/MultiCalc.App -f net10.0-android
```

`dotnet test` needs neither the workload nor a device. Only the app project does.

## Layout

```text
src/     one project per concern, see docs/architecture.md
tests/   mirrors src/, one test project per source project
  support/   fakes and helpers, not tests
docs/    the docs listed in the README
```

The test tree mirrors the source tree. `src/MultiCalc.Domain` is tested by
`tests/MultiCalc.Domain.Tests`, and the folders inside match too, so
`src/MultiCalc.Domain/Sessions/SessionBook.cs` is tested by
`tests/MultiCalc.Domain.Tests/Sessions/SessionBookTests.cs`. Finding the tests for a file
should never need a search.

## Conventions

**Project names are PascalCase.** The one place this repo departs from
lower-case-with-dashes. .NET project names double as assembly and namespace names and the
tooling assumes it. Folders that are not projects stay lower case.

**Central package versions.** All versions live in `Directory.Packages.props`. A
`PackageReference` in a csproj carries no `Version`. Shared build settings live in
`Directory.Build.props`.

**Do not pin the MAUI packages.** `Microsoft.Maui.Controls` has no entry in
`Directory.Packages.props` on purpose. `UseMaui=true` adds it implicitly at whatever
version the installed workload provides, and pinning a different one gets you a
`TypeLoadException` on `Microsoft.Maui.Controls.Page` the moment the app starts, with a
stack that points at `UseMauiApp` and tells you nothing. The web view package has to move
in step, which is why it uses `VersionOverride="$(MauiVersion)"`.

**Warnings are errors.** `TreatWarningsAsErrors` is on and every public type and member
needs an XML doc comment. Keep them to one line. The app and test projects opt out of the
docs requirement, since they are wiring rather than a library.

**Comments explain why, not what.** If a comment restates the code, delete it. The ones
worth keeping look like the note on `PercentExpander` explaining why percent means two
different things, or the one on `AndroidNativeCalculatorLauncher` explaining why the
launch mode has to be checked.

**No em dashes or en dashes** in code, comments or docs. Use a plain hyphen or rewrite.

## Tests

xunit.v3, run with `dotnet test`. .NET has no built in runner, which is the only reason
there is a test dependency at all.

Test files carry `using Xunit;` explicitly and classes are `sealed`.

Two rules about what gets tested:

- **Anything that decides something is tested.** `ExpressionDraft` holds every rule about
  which key may follow which, `PercentExpander` holds what percent means, `SessionBook`
  holds what happens when you close the last calculator. All three are pure values and
  all three have thorough tests.
- **Platform code is not unit tested.** `Platforms/Android` has to be exercised on a
  phone. Keep it thin enough that reading it is enough, and put anything that can be
  decided without the platform behind a seam so it can be tested on the other side.

`CalculatorStateTests` runs the whole New button decision, including the native path,
against `FakeNativeCalculatorLauncher`. That is the point of the seam: the behaviour that
differs by device is testable without a device.

When you fix a bug, add the test that would have caught it and note in one line what used
to happen:

```csharp
// Used to return "05" because a leading zero was treated as a digit rather than a placeholder.
```

## Adding a platform

iOS is the next target. What that needs:

1. Add the target framework to `src/MultiCalc.App/MultiCalc.App.csproj`.
2. Implement `INativeCalculatorLauncher` returning `NativeCalculatorStatus.Unsupported`.
   iOS has no way to launch another app's calculator twice, and the UI already handles
   that answer by hiding the option.
3. `MauiHaptics`, `MauiExternalBrowser`, `MauiFileExporter` and `MauiAppBuildInfo` are
   already cross platform and need no change.

Nothing in `Domain`, `Evaluation`, `Storage` or `Ui` should need touching. If it does,
something platform specific has leaked out of `App`.

## The support link

It must open the real browser, never a web view. Both stores read a payment page shown
inside an app as an in-app purchase that skipped their billing, and refuse the app. That
is why `MauiExternalBrowser` passes `BrowserLaunchMode.External` and why
`AndroidManifest.xml` declares an `https` intent under `<queries>`; without the
declaration the call silently fails on Android 11 and later.

The wording next to the link says plainly that it buys nothing. Leave it in.
