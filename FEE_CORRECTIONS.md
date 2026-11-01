# Korrigjimi i tarifës së një fature

Administratori hap kartelën financiare dhe zgjedh **Korrigjo tarifën** në rreshtin e faturës. Shkruan tarifën përfundimtare dhe arsyen. Për faturën 50.00 me pagesë 30.00, tarifa e re 30.00 e çon borxhin në 0.00. Pagesa dhe ngarkesa origjinale ruhen; diferenca regjistrohet si `MonthlyFeeAdjustment`, me autor, kohë, arsye dhe vlerat para/pas në historikun financiar.

Veprimi vlen vetëm për faturën e zgjedhur. Për vazhdimin me 30.00, përdorni **Ndrysho kategorinë** dhe zgjidhni tarifën individuale përkatëse nga muaji i ardhshëm. Nëse nuk ekziston, administratori e krijon te **Tarifat mujore**. Mos ndryshoni çmimin e kategorisë së përbashkët 50.00 për të korrigjuar vetëm një lojtar.

Korrigjimi lejohet edhe pas pagesës së pjesshme. Tarifa e re nuk mund të jetë nën shumën e mbuluar. Faturat sezonale, ato me lirim aktiv dhe lojtarët me pagesë sezonale në pritje përjashtohen. Formularët e vjetruar dhe ridërgimet refuzohen nën të njëjtin bllokim transaksional si pagesat. Nuk nevojitet migrim i databazës.

Kontrolli i integrimit krijon dhe largon vetëm një databazë të veçantë prove:

```powershell
dotnet run --project tests/FeeCorrections.Checks -- .
```
