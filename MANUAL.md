# Manuali kontekstual i përdorimit

**Manuali i përdorimit** në menu hap qendrën me kërkim. Butoni **Ndihmë** hap udhëzimin e faqes pa humbur formularin aktual. **Më udhëzo** kalon nëpër fushat e formularit, shpjegon qëllimin, kufijtë dhe detyrueshmërinë, dhe e thekson fushën në ekran. Escape e mbyll udhëzimin; mbyllja rikthen fokusin.

Përmbajtja shqip në `Content/Help/manual.json` mbulon modulet dhe faqet e platformës, duke përfshirë hyrjen dhe portalin e prindit. Formularët identifikohen nga DOM-i i faqes, pa lexuar vlerat e plotësuara. Nuk ka shërbim të jashtëm AI, API key, kosto për pyetje ose ndryshime në databazë. Kërkimi normalizon diakritikat dhe përdor sinonime të përcaktuara; shfaq vetëm udhëzime ekzistuese. Nuk gjeneron përgjigje për pyetje jashtë manualit.

## Qasja

Qendra `/Help` kërkon hyrje. Udhëzimet filtrohen me politikat reale të controller-it dhe action-it GET; edhe lidhja direkte te një temë kontrollon të njëjtën qasje. Për klubet dhe zerimin kontrollohet gjithashtu pronësia e platformës. Manuali nuk anashkalon kontrollet e regjistrimeve, ekipit ose klubit. Prindi dhe faqja e hyrjes marrin vetëm ndihmën e tyre kontekstuale. Dokumentet e printimit nuk përmbajnë butona ose shtresa të ndihmës.

Udhëzuesi nuk dërgon formularë, nuk klikon butona veprimi, nuk ndryshon vlerat dhe nuk bën kërkesa rrjeti. Për të treguar një fushë mund të hapë përkohësisht seksionin e saj `details`; në fund e rikthen atë. Etiketat shfaqen me `textContent`, ndërsa Razor kodon tekstin e kërkimit dhe përmbajtjen.

## Mirëmbajtja

- Kur shtoni ose ndryshoni një faqe, përditësoni modulin përkatës dhe `Pages` në JSON. Përdorni `Details` për udhëzime specifike të veprimit, shembuj dhe kufizime.
- Përdorni emrat e fushave dhe butonave që ekzistojnë realisht. Rishikoni udhëzimet bashkë me ndryshimin e rregullit të biznesit.
- Për një fushë që kërkon shpjegim të veçantë, shtoni përshkrimin te `hints` në `wwwroot/js/help.js`. Ndryshimet nuk duhet të lexojnë vlerat e përdoruesit.
- Përmbajtja kopjohet automatikisht gjatë build/publish. Nuk ka migrim ose konfigurim të ri të prodhimit.

## Verifikimi

`tests/Manual.Checks` kontrollon mbulimin e çdo view jo-partial, kërkimin, hyrjen HTTP, politikat e adminit/trajnerit/shikuesit, temat e paautorizuara, kodimin HTML dhe ndihmën e prindit. Krijon një databazë prove të veçantë dhe e heq në fund. Lidhja e PostgreSQL për provën jepet në `ConnectionStrings__DefaultConnection`.

```powershell
dotnet run --project tests/Manual.Checks -- .
```

Për testet e shfletuesit jepni si argument të dytë `tests/Manual.Checks/browser.cjs`; nevojitet Playwright dhe Chromium. `PLAYWRIGHT_MODULE` dhe `MANUAL_CHROME` mund të tregojnë instalime ekzistuese, ndërsa `MANUAL_SCREENSHOTS` cakton dosjen e pamjeve. Testet provojnë desktopin dhe telefonin, tastierën, kërkimin, ruajtjen e vlerave dhe mungesën e kërkesave POST gjatë ndihmës.
