# Premium login implementation

The supplied written specification drives the layout. No reference photo was attached, so pixel-for-pixel comparison against that photo was not possible.

Desktop uses a 70/30 presentation/sign-in split. Mobile/tablet places sign-in first. The existing Razor login handlers, authentication, club registration, endpoints and database are unchanged. New copy uses `LoginLanguage` in Albanian and English.

All overview figures, match-minute rows and device screens are demonstration data. AI panels describe presentation concepts only; no AI service was added. Nutrition copy is educational and explicitly does not replace a professional.

Devices, module icons, progress bars, graphs and the food illustration are HTML/CSS/SVG. The coach and parent are separate generated raster assets, not a flattened website image. Existing UPNEXT logo assets remain unchanged.

## Generated assets

Built-in `image_gen` mode, transparent background, saved in the repository:

- `wwwroot/images/upnext/login-coach.png`
- `wwwroot/images/upnext/login-parent.png`

Coach prompt:

> Use case: photorealistic-natural. Asset for an existing European premium sports club management website, not a full website mockup. A fictional adult male football coach age 38 in a clean navy blue tracksuit, waist-up three-quarter portrait, holding and looking thoughtfully at a slim tablet angled slightly toward viewer. Realistic professional editorial photography, relaxed confident expression, natural hands, cool daylight with gentle blue rim light. Isolated cutout on genuinely transparent background. No scenery, no floating cards, no typography, no logos, no watermarks. Keep entire head arms and tablet inside frame. Tablet screen is neutral light blue, UI will be rendered separately in HTML. Portrait aspect ratio.

Parent prompt:

> Use case: photorealistic-natural. Separate website cutout asset for premium European sports club management software. A fictional adult mother age 35 in smart casual cream jacket and blue top, waist-up three-quarter portrait holding a smartphone and looking at it with a calm warm smile. Professional natural editorial photography, believable hands and phone, cool soft daylight with gentle blue rim light. Genuinely transparent background. No scenery, no text, no logos, no watermark, no cards or website layout. Entire head arms and phone inside frame. Portrait aspect ratio.

## Validation

`browser.cjs` checks 1536 × 1024, 1280 × 1024, 768 × 1024 and 390 × 1024 in both languages, including image loading, module count, 70/30 desktop proportions, mobile login order, horizontal overflow, password visibility and reduced motion. Existing integration checks exercise actual login and pending club registration in an isolated temporary database. Screenshots remain in the local verification directory and are not published in chat.
