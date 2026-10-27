# Analiza e produktit për prezantim komercial

Analizë e kodit lokal, 22.10.2026. Prezantimi përshkruan funksione të implementuara në këtë version. Nuk përbën audit sigurie, verifikim ligjor, provë ngarkese ose konfirmim të versionit të publikuar në Railway. Aplikacioni dhe databaza nuk u ndryshuan për këtë prezantim.

## Produkti dhe audienca

Platformë web për klube futbolli dhe akademi që menaxhojnë lojtarë, familje, ekipe, trajnerë, pagesa, rekuizita, shërbime sportive dhe sponsorë. Klientët më të përshtatshëm kanë disa ekipe dhe staf që ndajnë përgjegjësi administrative e sportive. Moduli i rezervimeve mbulon edhe operimin e fushave. Nuk ka bazë për ta reklamuar si ERP universal ose si platformë të provuar për çdo sport.

## Struktura dhe arkitektura

- ASP.NET Core MVC me faqe Razor, CSS/JavaScript dhe pamje për telefon/tablet.
- ASP.NET Identity për llogaritë dhe rolet.
- EF Core me PostgreSQL, migrime dhe shërbime biznesi.
- Controller-at ekspozojnë kryesisht rrugë MVC GET/POST, jo një API publike të dokumentuar për integrues.
- Punëtorë në sfond për faturimin mujor, njoftimet dhe draftet e menusë.
- `Data/ApplicationDbContext.cs` përmban lidhjet kryesore dhe entitetet. `Program.cs` regjistron shërbimet, autentikimin dhe punëtorët.
- Inventari i skedarëve dhe veprimeve të kontrolluesve gjendet te `Presentation_Assets/Source_Inventory.txt`.

## Modulet e verifikuara

| Moduli | Aftësitë dhe rrjedha | Burimi kryesor |
|---|---|---|
| Lojtarët / familjet | Regjistrim individual ose familjar, ekip, tarifa, kontakt, kartelë financiare | StudentsController, FamiliesController, StudentWorkflowService, FamilyService |
| Tarifat | Kategori, histori çmimesh, tarifa familjare, lirim me arsye, muaj i parë dhe pagesa sezonale | TariffsController, TariffService, BillingService.FirstMonth/Seasonal |
| Ekipet | Trajner / ndihmëstrajner, mosha, lokacioni, oraret dhe caktimi i lojtarëve | TeamsController, TeamService, CoachSchedule |
| Prezenca | Regjistrim sipas ekipit/datës, historik, kontroll i konfliktit të ndryshimeve, ndalim prezence kur mjeku ka kufizuar stërvitjen | AttendanceController, AttendanceService, Models/Attendance.cs |
| Zhvillimi | Profil, pozita, matje, testime, aftësi teknike/taktike, portier, ndeshje/ngarkesë dhe grafikë | PlayerDevelopmentController/Service, DevelopmentRecord, PlayerEvaluation |
| Pagesat | Detyrime, parapagime, familje, sezonale, dëftesa dhe anulime me arsye | PaymentsController, BillingService dhe partials |
| Verifikimi bankar | Rresht në pritje, shuma reale dhe data, konfirmim/refuzim, ndarje e arkëtimit në detyrime | BankPaymentsController, BankVerificationService.* |
| Shpenzimet / arkat | Kategori, hyrje/dalje, anulime dhe histori; përmbledhje sipas periudhës | ExpensesController, TreasuryController, ExpenseService, TreasuryService |
| Rekuizitat / stoku | Produkte, variante/madhësi, hyrje, dalje te lojtari, arkëtim dhe gjurmë stoku | ProductsController, SalesController, InventoryService, IssueService, StockReportService |
| Trajnimet personale | Tarifa, caktim i lojtarit, orare, detyrime dhe arkëtime | PersonalTrainingController/Service |
| Fushat | Rezervime, seri, arkëtime, anulime, rol i veçantë për rezervime private | FieldsController, FieldService dhe SeriesPayments |
| Sponsorizimet | Plan vjetor, kategori, kontrata/afate, ndarje buxheti, pranime, fatura, paga, mbulim dhe raport sponsori | SponsorshipsController, SponsorshipService, Models/Sponsorship* |
| Kontrollet e sponsorëve | Pengon faturën me numër/furnitor të njëjtë dhe pagën e dytë për person/muaj; ndan lojtar/staf | SponsorshipService, veçanërisht validimi i Spend |
| Portali i prindit | Link/QR individual ose familjar, orar, ekip/trajner, pagesa, prezencë, ecuri, menu dhe udhëzime të ndara nga specialisti | ParentLinksController, ParentPortalController/Service, ParentProgressService |
| Njoftimet | Manuale dhe automatike për stërvitje, ndryshime orari, pagesa dhe ndryshim menuje | ParentNotificationsController, ParentAutomationService, ParentPushWorker, MenuNotificationService |
| Kujdesi | Caktim specialistësh, referim nga trajneri, shënime private, udhëzime dhe kufizim stërvitjeje | CareStaffController, CareReferralsController, PlayerCareController/Service |
| Ushqyerja | Menu bazë, menu individuale, draft Gemini dhe aprovimi para publikimit te prindi | NutritionMenuService, CoachMenuService, GeminiMenuService, NutritionDraftWorker |
| Raportet | Hyrje/shpenzime, detyrime/stok, veprime me arsye, sponsor, paga dhe aktivitet trajnerësh | ReportsController, ReasonReportService, CoachActivityService, SponsorshipsController |
| Identiteti i klubit | Emri, logoja dhe të dhënat e ndryshueshme të klubit | ClubSettingsController, ClubBranding, ClubSettings |
| Administrimi | Përdorues, role, staf, caktime dhe veprime administrative të mbrojtura | UsersController, UserAdministrationService, DataResetController |

## Rolet dhe kufijtë

- Super Admin: moduli i sponsorizimeve kërkon këtë rol. Në konfigurimin ekzistues mban edhe Admin.
- Admin: administrimi, konfigurimi, stafi dhe raporti i aktivitetit të trajnerëve.
- Operator: punë operative sipas `Roles.Write`; nuk është administrator i plotë.
- Shikues: rrugët me `Roles.Read`, pa të drejta të përgjithshme shkrimi.
- Trajner: ekipet/lojtarët e caktuar, prezenca, zhvillimi, referimet dhe propozimet e menusë sipas autorizimeve.
- Mjek / Nutricist: lojtarët e caktuar dhe shërbimet përkatëse. Të dhënat private nuk paraqiten të gjitha te trajneri ose prindi.
- RezervimePrivate: qasje e kufizuar në fushat/rezervimet përkatëse.
- Prindi: link i mbrojtur me token, për fëmijën/familjen, jo rol administrativ Identity.
- Nuk ekziston rol i veçantë Menaxher në `Roles`; për këtë prezantim drejtuesi përdor Admin sipas autorizimit.

## Rrjedhat që shpjegon prezantimi

1. Konfigurim klubi → ekipe/staf/tarifa → regjistrim lojtarësh/familjesh → operim → raporte.
2. Trajneri zgjedh ekipin → regjistron prezencën → sistemi ruan historikun → raportimi/prindi lexon informacionin e lejuar.
3. Detyrim → pagesë bankare në pritje → operatori fut shumën reale → vendim konfirmo/refuzo → ndarje dhe kartelë.
4. Plan sponsorësh → kontratë/buxhet → faturë/pagë → validim → shpenzim/raport. Ky regjistër ndahet nga shpenzimet operative.
5. Trajneri hap rast → specialisti shqyrton → udhëzime / kufizim stërvitjeje → kontroll prezence. Menuja ka rrjedhë të veçantë draft/aprovim/publikim.
6. Prindi hap linkun → sheh fëmijën/familjen → aktivizon njoftimet → merr njoftime kur pajisja dhe konfigurimi e lejojnë.

## Avantazhe komerciale të mbështetura

Lidhja e punës sportive me kartelën financiare, buxhetet e ndara të sponsorëve, verifikimi i shumës reale bankare, historiku i zhvillimit, lidhja e kufizimit mjekësor me prezencën dhe portali familjar. Këto janë aftësi të produktit, jo pretendime se konkurrentët nuk i kanë.

## Kufizime që nuk duhen fshehur

- Nuk provohet izolim multi-tenant mes klubeve në një databazë. Ndryshimi i emrit/logos nuk nënkupton shumë klube në një instancë. Vendosja dhe izolimi për klientin duhet të përcaktohen.
- Psikologu është parashikuar për më vonë; nuk prezantohet si modul klinik i implementuar.
- QR i parkingut është identifikues demonstrues; nuk është integrim me portë reale.
- Njoftimet varen nga konfigurimi push, abonimi/leja e prindit dhe mbështetja e pajisjes. Dërgimi nuk garanton lexim.
- AI kërkon çelës/model/kuotë të vlefshme. Drafti nuk është rekomandim klinik automatik dhe publikohet pas aprovimit.
- Raporti i trajnerëve numëron regjistrime unike të ruajtura dhe role aktuale. Nuk mat cilësinë e trajnerit apo çdo klikim në platformë.
- Nuk janë vërtetuar integrime bankare automatike, pagesa online, kontabilitet ligjor i plotë, SLA, certifikime ose garanci sigurie.
- Çmimet, afatet, mbështetja, migrimi dhe kushtet përcaktohen në ofertë. Në slajde përdoren vende të shënuara për plotësim.
- Përfitimet për kohën/koston janë arsyetim nga rrjedhat, jo rezultate numerike të matura.

## Pamjet dhe identiteti vizual

Ngjyrat nga `wwwroot/css/site.css`: navy #101d30, ink #17263d, yellow #f5ce4f. Logoja nga `wwwroot/images/fc-2-korriku.svg` është logo klubi, jo markë e veçantë e produktit; prezantimi e shënon si konfigurim shembull.

Pamjet `coach-screenshot.png` dhe `report-screenshot.png` renderojnë Razor Views origjinale të kompiluara, në një host të izoluar prezantimi pa databazë. Pamja e raportit ka emra anonimë dhe vlera ilustruese. Nuk janë prova të të dhënave ose rezultateve reale të klubit. Host-i nuk ngarkon Program.cs, seeders ose punëtorët e aplikacionit.

Fotografitë janë skena origjinale të gjeneruara me AI, jo fotografi të stafit, klientëve ose rezultateve reale. Promptet dhe përdorimi gjenden në Image_Generation_Prompts.md.
