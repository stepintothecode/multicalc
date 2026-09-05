# Requirements

What the app has to do, and the rules behind it.

## The two kinds of calculator

1. **In-app calculators.** As many as the limit allows, each with its own display and
   history. Always available, on every device and every platform.
2. **System calculators.** Extra copies of the calculator the phone shipped with, opened
   as separate tasks so they get their own Recents card.

## No setting for which kind

Both are buttons on the main screen, side by side, so there is nothing to configure and
nothing to explain on first run. An earlier build asked the question at startup and kept
the answer in Settings; that was a preference standing in for a button.

Where the phone will not open a second system calculator, that button is shown faded and
disabled with a line saying why, rather than hidden. A missing button looks like a bug;
a disabled one with a reason is information.

## One way in per feature

Every feature has exactly one control:

- switching, renaming, closing and adding calculators: the button at the top,
- the current calculator's history: the history button beside it,
- export, import and clearing everything: Settings,
- About and Support: the overflow menu.

Nothing appears twice. Two controls for one thing can disagree with each other, and the
one that is wrong is the one the person happens to press.

A setting that preferred system calculators on a device that stopped allowing them would
otherwise leave a dead button, so the New button falls back to an in-app calculator in
that case rather than doing nothing.

## Deciding whether the device allows it

Never assume, and never rely on one lookup. `CATEGORY_APP_CALCULATOR` is optional and
Samsung's calculator does not declare it, so the search runs three ways in order: the
category, then a list of manufacturer package names, then a scan of everything with a
launcher icon for something that calls itself a calculator.

Whatever is found, read its launch mode: `standard` and `singleTop` can be spawned,
`singleTask` and `singleInstance` cannot. Re-check whenever Settings opens, because the
person may have installed a different calculator.

## Calculating

- Standard arithmetic with brackets and correct precedence.
- Decimal arithmetic, not floating point: `0.1 + 0.2` must be `0.3`.
- Percent behaves the way a phone calculator does: after plus or minus it is a share of
  the left hand side, so `50 + 10%` is `55`; after times or divide it is a hundredth, so
  `200 * 10%` is `20`.
- Unclosed brackets are closed when equals is pressed.
- A failure names itself and leaves the typing alone. Never show a guessed number.

## History

- Per calculator, newest first, capped so storage stays bounded.
- Tapping a past result puts it back on the display.
- Clearable per calculator or all at once.
- Exportable to text, CSV or JSON through the platform's own save dialog, so no storage
  permission is needed.
- Importable from a JSON export. A calculator whose name matches one that is open has its
  entries merged; anything else arrives as a new calculator, keeping the name and colour it
  had. Nothing is overwritten or removed, and importing the same file twice changes nothing
  the second time.
- Only JSON imports. Text and CSV do not round trip, and half reading one would invent
  history that never happened.

## Colours

Every calculator carries a colour, so which one is open reads from the top bar without
reading its name. A new calculator takes the first quick colour nobody is using, and the
pencil in the list opens a picker: twenty four quick hues plus a slider for anything
between them.

A hue is stored, not a colour. Saturation and lightness belong to the theme, which is what
keeps every possible choice readable on both the light and the dark background. A free hex
could produce pale yellow on white, and nothing in the app could correct it.

## Settings

Theme, vibrate on key press, and the three history actions. That is the whole page. If a
thing can be a button where it is used, it does not also get a setting.

## Data

Everything stays in the app's private storage. No account, no network, no analytics. The
only way data leaves the device is an export the person asks for.

## Platforms

Android first, for Google Play. iOS later. Nothing in the shared code may assume Android,
and every platform capability sits behind an interface with a fake for tests.

## Out of scope

- Embedding the system calculator's screen inside this app. Android does not allow it:
  cross-app activity embedding requires the embedded app to opt in by naming the host's
  signing certificate, and the stock calculator does not.
- Adding a button to the system calculator's own interface. No supported mechanism exists
  on any mobile platform.
- Scientific functions, unit conversion, currency. A different app.
