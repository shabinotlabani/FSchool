# Sponsorizimet

Moduli gjendet te `/Sponsorships` dhe kërkon rolin `SuperAdmin`. Krijimi i rolit nuk promovon automatikisht asnjë llogari. Sipas kërkesës, caktimi i llogarisë së parë bëhet më vonë.

## Rrjedha e përdorimit

1. Regjistro planin vjetor: referenca e deklarimit, kategoritë, aktivitetet dhe shumat. Për pagat e stafit dhe lojtarëve shëno **Pagë mujore**.
2. Shto kontratat: sponsori dhe numri i biznesit, referenca, datat dhe ndarja e shumës sipas kategorive të planit. Shuma e ndarjeve duhet të barazohet me kontratën. Kontratat së bashku nuk mund të angazhojnë më shumë se plani i kategorisë.
3. Regjistro shumat e pranuara në bankë me referencën unike të transaksionit. Premtimi i kontratës nuk krijon para të disponueshme.
4. Regjistro përfituesit vetëm një herë, me kod të qëndrueshëm të personelit ose numër biznesi. Numri personal nuk kërkohet.
5. Regjistro pagesën bankare: kategoria, përfituesi, data, shuma, përshkrimi, dokumenti dhe referenca bankare. Për pagat kërkohet edhe muaji.
6. Hap kontratën për gjendjen sipas kategorive, pranimet, pagesat, historikun dhe raportin për printim.

## Faturat dhe propozimi i buxhetit

Te **Faturat → Faturë e re** regjistrohen përfituesi, dokumenti, data e pagesës, kategoria, shuma dhe referenca bankare. Nuk duhet zgjedhur paraprakisht sponsori. Kategoria, data dhe shuma rifreskojnë automatikisht listën e buxheteve. Propozimi i parë është kontrata e vlefshme që mbulon të gjithë shumën dhe përfundon më shpejt. Mund të zgjidhet edhe një propozim tjetër i vlefshëm.

Disponueshmëria është minimumi i buxhetit të mbetur të kategorisë, kufirit vjetor dhe mjeteve bankare të disponueshme nga data e pagesës e tutje. Kontratat jashtë afatit nuk propozohen. Shumat e pamjaftueshme shfaqen pa mundësi përzgjedhjeje; një faturë nuk ndahet automatikisht midis sponsorëve. Shërbimi i pagesës i rikontrollon të gjitha kufizimet kur ruhet fatura, edhe nëse gjendja ka ndryshuar pas propozimit.

Faturat ruhen në evidencën ekzistuese të sponsorizimeve; nuk krijojnë rreshta në shpenzimet e klubit dhe nuk hyjnë në arkën e shkollës. Nuk nevojitet migrim i ri për këtë rrjedhë. Lista e faturave përfshin edhe regjistrimet e mëparshme të bëra nga kontratat, me raport individual dhe anulim me arsye.

Pagat përdorin një kufizim unik për **përfitues + muaj**, për të gjithë sponsorët dhe kategoritë e pagave. Një pagë nuk ndahet në dy regjistrime apo dy sponsorë. Korrigjimi bëhet duke anuluar me arsye regjistrimin e gabuar dhe duke regjistruar pagesën e saktë. Kodi i personit dhe kategorizimi si pagë duhen përdorur saktë; ky kontroll nuk interpreton tekstin e faturave dhe nuk kontrollon pagesat jashtë modulit.

Faturat kontrollohen sipas përfituesit dhe referencës; referencat bankare dhe ridërgimi i të njëjtit formular nuk mund të krijojnë pagesa të dyfishta. Kontrollet janë transaksionale dhe kanë kufizime unike edhe në PostgreSQL. Kontrollet e fondeve shohin gjendjen për çdo ditë, që një pagesë me datë të kaluar të mos shpenzojë një pranim të ardhur më vonë.

Plani dhe kontratat mund të ndryshohen me arsye dhe referencë të ndryshimit. Formularët e vjetër refuzohen nëse regjistrimi është ndryshuar ndërkohë. Nuk lejohet ulja e kufijve nën angazhimet ose shpenzimet përkatëse, ndryshimi i sponsorit pas veprimeve financiare, ndryshimi i llojit të kategorisë së përdorur apo përjashtimi i pagesave ekzistuese nga periudha.

## Ndarja nga arka e shkollës

Sponsorizimet kanë evidencë të veçantë të mjeteve me destinim. Ato nuk rrisin shumën që operatorët mund të shpenzojnë nga arka e zakonshme dhe nuk përfshihen në raportin e saj. Pranimet dhe pagesat e sponsorëve duhen regjistruar vetëm në këtë modul. Moduli nuk kryen transferta bankare. Ruhet referenca e dokumentit; faturat dhe dëshmitë origjinale ruhen jashtë aplikacionit.

Kontratat lidhen me një plan vjetor; një marrëveshje shumëvjeçare regjistrohet sipas pjesëve vjetore përkatëse. Shumat shfaqen pa simbol monetar.

## Lejet dhe aktivizimi

`DbInitializer` krijon rolin gjatë nisjes së aplikacionit, pa ia caktuar një llogarie. Admin dhe Super Admin mund të zgjedhin **Super Admin** te krijimi ose ndryshimi i një përdoruesi. Kështu Admin mund ta caktojë edhe llogarinë e parë, përfshirë llogarinë e vet. Shërbimi cakton automatikisht të dy rolet `SuperAdmin` dhe `Admin` dhe rifreskon security stamp; përdoruesi duhet të hyjë përsëri për lejet e reja. Menaxhimi i një llogarie që tashmë është Super Admin mbetet vetëm për Super Admin.

Admin i zakonshëm nuk mund të ndryshojë profilin, fjalëkalimin ose rolin e një Super Admin. Super Admin nuk mund t'ia heqë rolin vetes; ruhet të paktën një Super Admin aktiv pasi të jetë caktuar i pari.

Migrimi `SponsorshipBudgets` shton vetëm tabelat e modulit. Nuk importon PDF-të, sponsorë realë, paga, pranime ose llogari. Dokumentet e dërguara shërbyen si referencë për strukturën e planit, afatin dhe kategoritë e kontratës. Moduli është evidencë financiare, jo verifikim automatik i miratimit shtetëror.

## Verifikimi

Ekzekuto kundër PostgreSQL lokal; testet krijojnë dhe heqin një bazë të veçantë:

```powershell
dotnet run --project tests/Sponsorships.Checks -c Sponsorship -p:UseAppHost=false -- "C:\Users\OnLine\Desktop\2 Korriku\2Korriku"
```

Testohen lejet, kufijtë vjetorë dhe të kontratës, datat, pranimi real i mjeteve, riprovimet, pagat dhe faturat e dyfishta, regjistrimet konkurruese, anulimet, ndryshimet me arsye dhe kufizimi unik në bazë. Roli nuk caktohet në bazën reale gjatë testeve.
