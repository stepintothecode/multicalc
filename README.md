# MultiCalc

More than one calculator, at the same time.

Phones give you one calculator. MultiCalc gives you as many as you need, and it does
it two different ways:

- **Calculators inside the app.** Each keeps its own display and its own history. The
  button at the top opens the list, where you switch, rename and close them; swiping the
  display sideways moves between them. This works on every device.
- **Copies of your phone's own calculator.** MultiCalc can ask Android to open a second
  or third copy of the calculator your phone shipped with. Those open outside the app
  and show up as separate cards in Recents.

Both sit side by side above the keypad, so there is nothing to set up and nothing to
choose between. Where a phone will not open a second system calculator, that button is
faded out with a line saying why.

Everything runs on the phone. No account, no server, no analytics.

## Install

With a phone plugged in and USB debugging on, this builds, installs and starts it:

```bash
dotnet build src/MultiCalc.App -f net10.0-android -t:Run
```

To get an APK you can copy around instead, ask for the assemblies to be packaged into it:

```bash
dotnet build src/MultiCalc.App -f net10.0-android -p:EmbedAssembliesIntoApk=true
adb install -r src/MultiCalc.App/bin/Debug/net10.0-android/com.stepintothecode.multicalc-Signed.apk
```

That flag is not optional for a hand-installed Debug build. Without it the APK ships
without its managed assemblies and expects the build to push them separately, so the app
dies on launch with `No assemblies found`. Release builds embed them anyway.

## The catch with system calculators

Android only lets one app open a second copy of another app when that app's main screen
is declared with a launch mode of `standard` or `singleTop`. Manufacturers choose that,
not you, so the answer differs by phone.

MultiCalc asks the device rather than guessing. Settings shows what it found, and the
option is hidden when the answer is no. Verified working on a Samsung Galaxy A55 running
Android 16, where the calculator opens as many separate Recents cards as you ask for.

## History

Each calculator keeps its own tape. Tap a past result to put it back on the display.

Settings exports every calculator's history to a file you choose, as text, CSV or JSON,
through Android's own save dialog, and imports a JSON export back. Importing merges by
calculator name and never overwrites or removes anything, so running it twice is safe.

## Docs

- [Requirements](docs/requirements.md), what the app has to do
- [Architecture](docs/architecture.md), how it fits together
- [Contributing](CONTRIBUTING.md), layout, tests and conventions
- [Play listing](docs/play-store-listing.md), everything Play Console asks for
- [Privacy policy](https://stepintothecode.github.io/multicalc/privacy/), the published
  page Google Play points at. Its source text is
  [docs/privacy-policy.md](docs/privacy-policy.md)

## Website

GitHub Pages serves this repository's root, so the landing page and the privacy policy
live alongside the code:

```text
.nojekyll            stops Pages running the files through Jekyll
index.html           https://stepintothecode.github.io/multicalc/
privacy/index.html   https://stepintothecode.github.io/multicalc/privacy/
assets/site.css      shared styling for both
```

Point Pages at the `main` branch, folder `/ (root)`. Google Play needs that privacy URL
live before it will accept the app.

## Licence

[MIT](LICENSE).

## Author

Built by [StepIntoTheCode](https://github.com/stepintothecode).
Free, ad free and tracker free.
[Support the project](https://stepintothecode.github.io/support/?from=multicalc-app).
