# Numri i lojtarëve sipas ligës

Te **Ligat → Edito → Formati i futbollit**, administratori zgjedh numrin e lojtarëve, duke përfshirë portierin: **8+1** ruhet si 9, **6+1** si 7 dhe **10+1** si 11.

Ndeshjet e reja dhe formacionet ende në përgatitje marrin formatin e ligës. Trajneri sheh numrin përkatës të vendeve në fushë; formatet me më pak se 11 lojtarë përdorin vendosjen automatike. Nuk lejohet më shumë titullarë ose lojtarë njëkohësisht në fushë se kufiri i ligës. Pas ndryshimit të formatit, një formular i hapur më herët duhet rihapur; trajneri rishikon titullarët nëse kufiri është ulur.

Ndeshjet e nisura ose të përfunduara ruajnë numrin me të cilin u konfirmua formacioni. Ndeshjet miqësore pa ligë ruajnë numrin e tyre të pavarur. Rregulli i ri zbatohet vetëm në futboll.

Migrimi `LeaguePlayersOnCourt` shton fushën në ligë me vlerë fillestare 11. Nuk ndryshon ndeshjet e ruajtura. Duhet aplikuar gjatë publikimit para përdorimit të versionit të ri.

Verifikimi: `dotnet run --project tests/LeagueFormats.Checks -- .` përdor një databazë të re prove dhe e largon në fund.
