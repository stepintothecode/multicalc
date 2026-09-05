# Google Play listing

Everything Play Console asks for, ready to paste. Sections match the Console's own
navigation.

Assets are generated, not hand made. Rerun them with:

```bash
python tools/make-icons.py                       # app icon, every size
python tools/make-store-assets.py <raw-shots>    # store icon, feature graphic, screenshots
```

---

## 1. Main store listing

### App name

Max 30 characters. Play rejects promotional words and emoji here.

```text
MultiCalc: Many Calculators
```

27 characters. If that name is taken, `MultiCalc - Split Calculator` (28) or
`Multi Calculator Workspace` (26) are free of the same collision risk.

### Short description

Max 80 characters. This is the line under the title in search results.

```text
Run several calculators at once, each with its own display and history.
```

70 characters.

### Full description

Max 4000 characters. Plain text; Play strips most formatting.

```text
Your phone gives you one calculator. MultiCalc gives you as many as you need.

Keep the grocery total, the rent split and the invoice going at the same time, without
losing one to start the next. Each calculator keeps its own display, its own history and
its own colour, so you always know which one you are looking at.


TWO KINDS OF CALCULATOR

In this app
Adds another calculator to your list. Each keeps its own display and history. Name them,
colour them, and switch between them from the button at the top or by swiping the
display sideways. Works on every phone.

Phone calculator
Opens another copy of the calculator your phone already came with. It opens outside
MultiCalc as its own card in Recents, so you switch to it the way you switch to any
other app. Whether a phone allows this is up to its manufacturer. MultiCalc asks your
device directly and turns the button off, with the reason, where the answer is no.


BUILT FOR THUMBS

Round keys with real space between them, so a slightly off tap still lands where you
meant. Keys respond the moment your finger touches down, and slide from one to the next
to enter several in one movement, the way your phone's own calculator does.


A HISTORY PER CALCULATOR

Every calculator keeps its own tape. Tap any past result to put it straight back on the
display. Export the lot to a text, CSV or JSON file whenever you want, and import a
JSON export back later. Importing merges by name and never overwrites anything.


DECIMAL ARITHMETIC THAT ADDS UP

0.1 plus 0.2 is 0.3, not 0.30000000000000004. Brackets, correct operator precedence, and
percent that behaves the way a calculator should: 50 + 10% is 55, and 200 x 10% is 20.


NO ACCOUNT, NO SERVER, NO ADS

MultiCalc has no internet permission at all. Nothing you type can leave your phone,
because there is no way for it to. No account, no analytics, no advertising, no
tracking. Your calculators live in the app's private storage and nowhere else.

Light, dark and system themes. Free and open source under the MIT licence.
```

Roughly 1,950 characters.

### Graphics

| Asset | Play requirement | File |
| --- | --- | --- |
| App icon | 512 x 512 PNG, 32-bit, under 1 MB | `assets/store/icon-512.png` |
| Feature graphic | 1024 x 500 PNG or JPEG | `assets/store/feature-1024x500.png` |
| Phone screenshots | 2 to 8, 16:9 or 9:16, sides 320 to 3840 px | `assets/store/screenshots/*.png` (six, 1080 x 1920) |
| 7 inch tablet | optional, 2 to 8 | not supplied, see below |
| 10 inch tablet | optional, 2 to 8 | not supplied, see below |
| Promo video | optional YouTube URL | none |

**On tablets.** MultiCalc is locked to portrait and designed for a phone, so no tablet
screenshots are supplied. Play will mark the listing "not optimised for tablets" on
large screen devices. That is accurate rather than a mistake, and it does not block
publishing. Supplying tablet screenshots of a portrait phone layout would be worse.

---

## 2. Store settings

| Field | Value |
| --- | --- |
| App or game | App |
| Category | Tools |
| Tags | Calculator, Productivity, Utilities |
| Email address | **required by Play, and shown publicly on the listing.** Type it into the Console; it is deliberately not written down in this repository |
| Website | https://stepintothecode.github.io/multicalc/ |
| Phone | leave blank, it is optional and becomes public |

Play makes the developer email address mandatory and displays it on the store page, so
there is no way to ship without one. A forwarding address you can retire later is worth
considering, since it becomes public the moment the listing does. Nothing else in this
repository, the app or the privacy policy carries an address: the policy points at GitHub
issues instead.
| External marketing | Opt out is fine; it only affects Google promoting the app |

---

## 3. App content

Every item under Policy, App content. All of these must be green before you can roll out.

### Privacy policy

Required for every app, whether or not it collects data.

```text
https://stepintothecode.github.io/multicalc/privacy/
```

**Note the trailing slash and the absence of a file name.** GitHub Pages serves a repo at
`https://<user>.github.io/<repo>/` from the repository root, so a page lives at
`privacy/index.html` and is reached at `/privacy/`. A path like `/privacy.html` only
works if a file of exactly that name sits at the root.

The pages are in the repository and go live as soon as Pages is pointed at the root of
the `main` branch:

```text
.nojekyll            stops Pages running the files through Jekyll
index.html           the landing page, at /multicalc/
privacy/index.html   the policy, at /multicalc/privacy/
assets/site.css      shared styling for both
```

Same layout as the housie repo, so the two sites behave the same way.

**Publish it before submitting.** Play fetches the URL and rejects the app if it 404s.
Open it in a browser first: Pages can take a couple of minutes on the first deploy, and
it returns 404 until then.

### Ads

> Does your app contain ads?

**No.** The app has no advertising SDK and no internet permission.

### App access

> Is all or some functionality restricted?

**All functionality is available without special access.** No login, no region lock, no
paywall. Nothing to give the reviewer.

### Content rating

Answer the IARC questionnaire as a **Utility, Productivity, Communication or Other** app.
Every content question is No:

| Question | Answer |
| --- | --- |
| Violence, sexuality, language, controlled substances | No to all |
| Crude humour, horror, gambling or simulated gambling | No to all |
| Does the app let users interact or exchange content? | No |
| Does the app share the user's location with other users? | No |
| Does the app allow purchase of digital goods? | No |
| Does the app contain any user generated content? | No |
| Is the app a web browser or search engine? | No |
| Does the app collect or share personal information? | No |
| Does the app contain a link to an external website? | **Yes**, the Support link opens a browser |

Expected outcome: Everyone / PEGI 3 / USK 0 / ESRB Everyone.

### Target audience and content

- **Age groups:** 13 to 15, 16 to 17, and 18 and over.
- **Appeals to children:** No.

Selecting an under-13 group puts the app in the Families programme, which brings extra
review and ongoing obligations. MultiCalc would pass them all, but there is no benefit
in signing up for them. If you would rather have younger students in the audience,
select 5 to 8 and 9 to 12 as well and expect a slower first review.

### Data safety

The one that takes longest in the Console. Every answer here is straightforward because
the app has no internet permission.

| Question | Answer |
| --- | --- |
| Does your app collect or share any of the required user data types? | **No** |
| Is all of the user data collected by your app encrypted in transit? | Not applicable, nothing is collected |
| Do you provide a way for users to request that their data is deleted? | Not applicable, nothing is collected |

Then, because the answer to the first question is No, the whole data type tree stays
empty. Do not tick anything under Location, Personal info, Financial info, App activity,
App info and performance, or Device or other IDs.

**Why "No" is correct here, if you are ever asked to justify it.** Play defines
"collected" as data transmitted off the device. MultiCalc writes calculators, history
and settings to Android's app-private storage and nothing else. The Release build
declares exactly one permission, `VIBRATE`. There is no `INTERNET` permission, so
transmission is not merely absent by policy, it is impossible. History export writes a
file the person chooses through Android's own save dialog, which is a user action, not
collection.

### Other declarations

| Declaration | Answer |
| --- | --- |
| Is this a news app? | No |
| Is this a COVID-19 contact tracing or status app? | No |
| Does the app have government affiliations? | No |
| Financial features | None of these |
| Health apps | Not a health app |
| Advertising ID | **Not used.** The app does not declare `com.google.android.gms.permission.AD_ID` |

---

## 4. Building what you upload

Play takes an **Android App Bundle**, not an APK. Everything shipped so far has been a
Debug APK for testing on your own phone.

### One time: make an upload key

Keep this file and its passwords somewhere you will still have them in five years.
Losing it means you cannot update the app under the same listing without asking Google
to reset the upload key.

```bash
keytool -genkeypair -v ^
  -keystore multicalc-upload.keystore ^
  -alias multicalc ^
  -keyalg RSA -keysize 2048 -validity 10000
```

Store it outside the repository. `.gitignore` already excludes `*.keystore` and `*.jks`,
but the safest place is not the working tree at all.

### Every release

```bash
dotnet publish src/MultiCalc.App -f net10.0-android -c Release ^
  -p:AndroidKeyStore=true ^
  -p:AndroidSigningKeyStore=<path>\multicalc-upload.keystore ^
  -p:AndroidSigningKeyAlias=multicalc ^
  -p:AndroidSigningStorePass=<store password> ^
  -p:AndroidSigningKeyPass=<key password>
```

The bundle lands in `src/MultiCalc.App/bin/Release/net10.0-android/publish/` as
`com.stepintothecode.multicalc-Signed.aab`. That is the file you upload.

Release builds trim unused code, which Debug builds do not. Install the Release build on
a phone once before you upload it, not just the Debug one.

### Version numbers

Both live in `src/MultiCalc.App/MultiCalc.App.csproj`:

- `ApplicationDisplayVersion` is the version people see, currently `1.0`
- `ApplicationVersion` is the integer build number, currently `1`

Play refuses a bundle whose `ApplicationVersion` is not higher than the last one you
uploaded. Raise it every single time, even for a rejected upload.

---

## 5. Release notes

Max 500 characters per language. For the first release:

```text
First release.

Run several calculators at once, each with its own display, history and colour. Open
extra copies of your phone's own calculator too, where your phone allows it. Round keys
you can slide across, a history tape per calculator, export and import, and light, dark
and system themes.

No account, no ads, no internet permission. Everything stays on your phone.
```

---

## 6. Before you press publish

- [ ] `https://stepintothecode.github.io/multicalc/privacy/` opens in a browser
- [ ] Release bundle installed and opened on a real phone, not just the Debug build
- [ ] `ApplicationVersion` raised
- [ ] Upload keystore backed up somewhere you will not lose it
- [ ] App name checked against Play search for a collision
- [ ] Screenshots reviewed at phone size, not just on a monitor

Two things worth knowing about the first submission: review of a new developer account
commonly takes several days rather than hours, and Play requires new personal developer
accounts to run a closed test with testers before production access opens up. Check the
current rule in the Console, because it has changed more than once.
