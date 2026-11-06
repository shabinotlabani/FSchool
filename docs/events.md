# Eventet dhe lejet prindërore

Moduli gjendet te **Eventet** në menunë e klubit. Administratori menaxhon kategoritë, krijon eventin dhe zgjedh lojtarë, ekipe ose grupmosha. Ftesat krijohen si një listë e ruajtur; ndryshimi i ekipit të lojtarit nuk ia kalon automatikisht ftesën dikujt tjetër.

## Rrjedha e punës

1. Te **Kategoritë**, shtoni kategoritë fillestare ose kategori të klubit.
2. Krijoni eventin, përfshirë afatin, kapacitetin, çmimin, kushtet dhe politikën e anulimit. Fotografia është opsionale.
3. Publikoni eventin. Ftesa shfaqet te **Eventet & Lejet** në portalin e prindit dhe njoftimi vendoset në sistemin ekzistues push për pajisjet aktive.
4. Prindi zgjedh fëmijën, shkruan emrin e vet dhe pranon ose refuzon pjesëmarrjen. Për konfirmim kërkohet pranimi i kushteve. Linku privat identifikon qasjen; emri i kujdestarit është emri i deklaruar në formular.
5. Konfirmimi krijon detyrimin e eventit. Publikimi, refuzimi, mungesa e përgjigjes dhe eventet falas nuk krijojnë detyrim.
6. Regjistroni pagesat e pjesshme ose të plota te pjesëmarrësi. Pagesat bankare, shpenzimet bankare dhe rimbursimet bankare kalojnë përmes **Verifikimit të bankës**.
7. Regjistroni prezencën gjatë eventit dhe printoni listën e pjesëmarrësve të konfirmuar. Eventi mund të përfundohet pas orës së përfundimit.

Pamja e eventeve mbështet tabelë, kalendar dhe karta. Tabelat financiare ruajnë totalet e përbashkëta të platformës dhe lëvizjen horizontale në telefon.

## Autorizimet dhe ndryshimet

Çdo përgjigje ruan fëmijën, emrin e deklaruar të kujdestarit, gjurmën hash të linkut, datën dhe një kopje të kushteve, vendit, datave, çmimit, transportit dhe politikës së anulimit. Tokeni privat nuk kopjohet në historik. Familjet japin përgjigje të ndara për çdo fëmijë.

Ndryshimi i informacioneve kryesore të një eventi të publikuar rrit versionin dhe kërkon konfirmim të ri. Detyrimi i mëparshëm lirohet deri në pranimin e ri; pagesat mbeten të regjistruara. Formularët për versionin e vjetër refuzohen.

Prindi mund të kërkojë tërheqjen e pëlqimit. Administratori pranon ose refuzon kërkesën me arsye; mund të anulojë edhe një pjesëmarrje individuale me arsye. Historiku ruhet.

## Financat dhe anulimet

- Një çift event–lojtar ka vetëm një ftesë dhe detyrim. Konfirmimet, pagesat dhe kthimet serializohen me bllokimin ekzistues financiar; formularët financiarë kanë çelësa idempotence.
- Borxhi i eventit mbetet i ndarë nga mujorja. Parapagimi mujor nuk shlyen vetvetiu eventet dhe pagesa e eventit nuk shlyen mujoren.
- Politika **Lirohet detyrimi** krijon një shumë të kthyeshme për pagesat e kryera. Administratori mund ta rimbursojë ose ta kreditojë në llogarinë mujore.
- Politika **Ruhet tarifa** ruan tarifën e autorizuar pas anulimit. Lojtarët që nuk kanë konfirmuar nuk marrin detyrim.
- Rimbursimi krijon shpenzim real në librin ekzistues. Kreditimi është transferim i brendshëm dhe nuk krijon arkëtim fiktiv. Rimbursimi nuk mund të anulohet veçmas si një shpenzim i zakonshëm.
- Lidhni shpenzime ekzistuese ose krijoni shpenzime nga eventi. Sponsorizimet lidhen me arkëtimet ekzistuese të sponsorëve; i njëjti arkëtim i plotë nuk mund t'u atribuohet dy eventeve.
- Rezultati financiar = pagesat neto të eventit + sponsorizimet e pranuara − shpenzimet aktive. Pagesat neto përjashtojnë kthimet dhe kreditimet; borxhet e pambuluara shfaqen veçmas.

Njoftimet përdorin outbox-in dhe punëtorin ekzistues push. Kujtesat për afatin dhe fillimin janë idempotente; një kujtesë e vjetruar nuk dërgohet pasi ftesa është trajtuar ose versioni ka ndryshuar. Portali shfaq kërkesat dhe gjendjen edhe pa aktivizuar push në pajisje.

## Migrimi dhe kontrolli

Migrimi `ClubEventsAndParentConsents` shton tabelat e modulit dhe lidhjen e arkëtimeve me ftesat. Filtrat, çelësat e përbërë dhe kontrolli i shkrimeve sipas klubit përdorin të njëjtën infrastrukturë si modulet ekzistuese. Migrimi aplikohet nga nisja normale e aplikacionit.

Kontrollet e integrimit përdorin një databazë të re me emër unik, të cilën e heqin pas testimit:

```powershell
dotnet run --project tests/Events.Checks -- "C:\path\to\2Korriku"
```

Lidhja e testimit merret nga `ConnectionStrings__DefaultConnection`, ose nga konfigurimi lokal. Përdorni një instancë lokale testimi me leje për krijimin e databazave.
