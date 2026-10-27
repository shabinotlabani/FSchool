# Kontratat e lojtarëve dhe stafit

Admin: Kontratat -> Kontratë e re. Regjistrimi i lojtarit kërkon ekipin dhe ofron vetëm shënimin Me kontratë. Lojtari ruhet Në pritje të kontratës pa detyrim mujor; dokumenti dhe paga plotësohen më vonë nga lista Kontratat. Aktivizimi e heq statusin në pritje. Aktivizimi kërkon PDF të nënshkruar (deri 5 MB); dokumenti ruhet në databazë, jashtë wwwroot, dhe shkarkohet vetëm nga admini i klubit. Drafti nuk aktivizon listëpagesë ose lirim të ri; lirimi i regjistrimit në pritje mbetet deri në aktivizim. Stafi regjistrohet veçmas te Kontratat → Stafi: menaxhment, staf teknik, shëndetësor, administratë ose tjetër; nuk kërkohet lidhje me ekipin. Kontrata e re hapet pa zgjedhjen Lojtar / Staf. Për lojtarin e regjistruar, hapja nga lista në pritje ruan lidhjen ekzistuese automatikisht. Stafi lidhet me përfituesin e pagave, pa krijuar lojtar ose llogari me detyrime mujore. Kontratat aktive janë të pandryshueshme; rinovimi bëhet me kontratë të re dhe periudhat nuk mbivendosen. Përfundimi i parakohshëm ruan arsyen dhe historikun.

## Faturimi

Lirimi vlen për të gjithë muajt kalendarikë që preken nga periudha efektive e kontratës. Muajt e faturuar korrigjohen me regjistrim MonthlyFeeAdjustment, jo me fshirje. Pagesat e mëparshme mbeten në llogari (mund të krijojnë parapagim). Detyrimet para kontratës, rekuizitat dhe trajnimet private nuk falen automatikisht. Pas skadimit rikthehet tarifa ekzistuese. Paketat familjare dhe sezonale duhet të zgjidhen para aktivizimit; nuk përzihen me kontratën.

## Pagat

Aktivizimi lidh një përfitues unik të sponsorizimeve me lojtarin. Lista vjetore shfaq edhe muajt pa pagesë; totalet financiare vazhdojnë të përfshijnë vetëm pagesat reale. Një përfitues/muaj nuk mund të paguhet dy herë nga sponsorë të ndryshëm. Përllogaritja propozon neton sipas ditëve kalendarike të mbuluara në muaj; bonuse, mungesa, shtesa dhe pagesa faktike kontrollohen nga financa para regjistrimit. Pagat bruto/neto dhe eksporti CSV janë plan pune, jo skedar i certifikuar për import në EDI. Pagesa e tatimit dhe pensioneve nuk krijohet automatikisht nga kontrata.

## Burimet dhe kufiri i integrimit

- Përmbajtja e kontratës: Ligji 03/L-212, nenet 7, 11, 20 dhe 71: https://gzk.rks-gov.net/ActDocumentDetail.aspx?ActID=2735
- Normat mujore nga gushti 2024: 0 deri 250, 8% për pjesën 250–450, 16 + 10% mbi 450 të bazës së tatueshme. Punëdhënësi sekondar: 10%. Kontributet standarde: 5% punëmarrësi dhe 5% punëdhënësi; përjashtimet kanë fushë arsyetimi. https://www.atk-ks.org/portfolio/informata-te-pergjithshme-per-tatimet-ne-kosove/
- Fillimi i zbatimit: https://www.atk-ks.org/njoftim-shtese-lidhur-me-aplikimin-e-shkalleve-te-reja-per-tatimin-ne-te-ardhurat-personale/
- Tatimi/kontributet deklarohen sipas periudhës së pagesës, jo thjesht muajit të kontratës: https://www.atk-ks.org/pyetje-te-shpeshta/?wpfaqpage=150

Fusha e datës/referencës ATK evidenton regjistrimin e bërë jashtë platformës. Nuk ka lidhje API me EDI dhe as deklarim automatik. Formulari ruan të dhënat kryesore dhe dokumentin origjinal; nuk vërteton ligjshmërinë e çdo klauzole, minimumin e pagës, statusin e rezidencës, lejen e punës, marrëveshjet tatimore ndërkombëtare apo rregullat e federatës. Këto mbeten për kontrollin e klubit dhe kontabilistit para nënshkrimit/deklarimit. Rregullat për të mitur nuk nënkuptojnë miratim të çdo pune sportive; duhet plotësuar legjislacioni për punët e lehta dhe mbrojtjen e të miturve.
