# Fshirja e ndeshjeve

Administratori zgjedh **Fshi** te lista javore ose lista e ndeshjeve zyrtare, ose **Fshi ndeshjen** te detajet/editimi. Faqja e konfirmimit paraqet datën, ekipin, kundërshtarin, garën dhe statusin. Vetëm butoni **Po, fshije ndeshjen** kryen fshirjen.

Fshihen ndeshja, formacioni, minutat, ngjarjet, vlerësimet dhe historiku i saj. Lojtarët dhe ekipet mbeten. Fshirja nuk kthehet. Nëse ekziston një shpenzim i lidhur, edhe i anuluar, fshirja ndalohet për të ruajtur evidencën financiare.

Veprimi kërkon rolin Admin dhe token antiforgery. Filtrat e klubit, revisioni dhe bllokimet transaksionale mbrojnë nga qasja mes klubeve, formularët e vjetruar dhe ndryshimet e njëkohshme. Nuk ka migrim të ri.

Testet përdorin vetëm një databazë të re të izoluar: `dotnet run --project tests/MatchDeletion.Checks -- .`.
