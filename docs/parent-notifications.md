# Njoftimet për prindërit

Familjet përdorin një lidhje dhe QR të përbashkët, të krijuar nga **Linku i prindit** i cilitdo anëtar aktual. Lidhjet e vjetra individuale mbeten vetëm për atë fëmijë; nuk marrin automatikisht qasje te motrat/vëllezërit. Portali lejon zgjedhjen e fëmijës dhe përdor gjithmonë anëtarësimin aktual, edhe kur shikohet një muaj i kaluar. Aktivizimi i njoftimeve nga lidhja familjare përfshin fëmijët aktivë aktualë; pas shtimit të një anëtari të ri, hapeni portalin dhe aktivizoni përsëri nëse kërkohet. Dalja nga familja ose rikrijimi i lidhjes ndalon dërgimin përmes autorizimit të vjetër.

1. Publikoni versionin e ri; migrimet krijojnë tabelat e njoftimeve në PostgreSQL.
2. Si Admin hapni **Njoftimet për prindërit**. Shkruani adresën publike HTTPS të platformës dhe shtypni **Aktivizo njoftimet**. Çelësat VAPID krijohen një herë dhe ruhen në databazë, jo në kod. Nuk duhen variabla të reja në Railway. Ruani databazën dhe kopjet e saj rezervë; ndryshimi i çelësave kërkon riabonim të pajisjeve.
3. Prindi hap lidhjen e vet, lexon njoftimin për pagesën kur ka, pastaj shtyp **Aktivizo njoftimet** dhe lejon njoftimet e telefonit. Në iPhone kërkohet iOS 16.4+ dhe hapja nga ikona pasi portali të jetë shtuar në Home Screen.
4. Admin shkruan titullin dhe mesazhin, zgjedh ekipin ose lojtarin dhe konfirmon dërgimin. Pa përzgjedhje dërgohet te të gjithë abonentët aktivë. Njoftimet dërgohen vetëm manualisht në këtë version.

Historiku tregon pranimin nga shërbimi push, jo leximin nga prindi. Dërgimi përpunohet nga punëtori i aplikacionit, me tentime të kufizuara dhe radhë në databazë. Railway duhet ta mbajë shërbimin aktiv; nëse fle ose është i ndalur, radha pret nisjen e tij. Rrjeti, lejet dhe cilësimet e telefonit mund ta vonojnë shfaqjen.

Lidhjet e revokuara dhe lojtarët joaktivë përjashtohen. Çaktivizimi anulohet vetëm për lojtarin dhe pajisjen përkatëse, pa çaktivizuar motrat/vëllezërit në të njëjtin shfletues. Endpoint-et lejohen vetëm për shërbimet e mbështetura të Google, Apple, Mozilla dhe Windows; ridrejtimet HTTP nuk ndiqen.

Verifikimi real pas publikimit: aktivizoni nga një pajisje Android dhe një iPhone i instaluar në Home Screen, mbyllni portalin, dërgoni një njoftim vetëm te lojtari provues dhe kontrolloni shfaqjen dhe hapjen e lidhjes së saktë. Pastaj çaktivizoni njoftimet dhe kontrolloni që pajisja të mos përfshihet në dërgimin tjetër. Mos përdorni dërgimin për të gjithë për testim.
