# Doručení teleportem – plánování nakládky dodávek do AlzaBoxů

Program pro jeden okruh rozhodne, které zásilky naložit do kterých dodávek, aby byla výnosnost co nejvyšší. Ke každému plánu navíc spočítá **horní mez výnosu**, takže je vidět, o kolik nejvýš může plán zaostávat za optimem.

```
dotnet run -c Release --project Alza   # scénáře, časy a srovnání s jednoduššími pravidly
dotnet test                            # testy
```

Potřebuje .NET 10 SDK.

## Jak jsem problém pochopil

- Na jeden okruh je k dispozici 120 dodávek po 7 m³ a 5,5 t, dohromady **840 m³ a 660 t**. Dodávky jezdí dvakrát denně a každý okruh plánuji zvlášť (viz [Dvě jízdy denně](#dvě-jízdy-denně)).
- Mimo úterý a čtvrtek je zásilek víc, než se vejde, a je potřeba vybrat ty, které přinesou nejvíc. V úterý a ve čtvrtek se vejde všechno a jde jen o to všechno naložit.
- Matematicky jde o **problém batohu s více batohy a dvěma omezeními** (multiple multidimensional knapsack). Je NP-těžký, takže přesné řešení pro statisíce zásilek a 120 dodávek v krátkém časovém okně nepřipadá v úvahu. Potřebuji rychlou heuristiku – a způsob, jak změřit, jak je dobrá.

Řešení stojí na dvou pozorováních:

1. **Zásilka je proti dodávce malá.** Do dodávky se vejdou stovky zásilek, takže se 120 dodávek chová skoro jako jedna velká. V takové situaci je hladový výběr podle výnosu na jednotku místa velmi blízko optima.
2. **Hmotnost začne omezovat až u hustého nákladu.** Limit 5,5 t na 7 m³ odpovídá průměrné hustotě asi **786 kg/m³**, což je víc než u běžných zásilek – obvykle tedy dřív dojde objem. Řešení na to ale nespoléhá: u těžkého zboží omezuje hmotnost a u smíšeného obojí zároveň.

## Řešení

Hlavní plánovač je `AdaptiveGreedyPlanner`. Pro každý okruh:

1. **Vyřadí zásilky, které se nevejdou ani do prázdné dodávky.** Ty zůstanou ve skladu.
2. **Pozná slabý den.** Pokud se celkový objem i hmotnost vejdou do flotily, nic nevybírá a nakládá od největších zásilek, aby se vešlo všechno. Den v týdnu se nikde nenastavuje – plánovač to pozná z dat.
3. **Seřadí zásilky podle výnosu na spotřebovanou kapacitu.** Zásilka spotřebuje dva zdroje, proto je kapacita jedno číslo, které je kombinuje:

   `kapacita = s · hmotnost / 5 500 kg + (1 − s) · objem / 7 m³`

   **Podíl hmotnosti `s`** říká, nakolik se počítá hmotnost (0 = jen objem, 1 = jen hmotnost).
4. **Zvolí `s` podle zásilek daného okruhu** – hodnotu, pro kterou vyjde nejtěsnější horní mez výnosu (bod 6). Zkouší nejdřív hodnoty s krokem 0,1, pak s krokem 0,01 kolem nejlepší, a body počítá paralelně. Pro běžné zásilky vyjde 0, pro těžké zboží 1, pro smíšené hodnota mezi tím.
5. **Nakládá hladově.** Zásilky v tomto pořadí vkládá do dodávky, jejíž volná kapacita zásilce nejlépe odpovídá (pravidlo skalárního součinu z vektorového bin packingu): malá těžká zásilka jde tam, kde zbývá nosnost, objemná lehká tam, kde zbývá místo. Zásilka, která se nevejde nikam, zůstane ve skladu a mezery zaplní menší zásilky dál v pořadí.
6. **Spočítá horní mez výnosu.** Každý platný plán dodrží celkovou nosnost i objem flotily, a tedy i jejich kombinaci podle libovolného `s`. Pro jediné omezení se snadno spočítá nejlepší „zlomkové“ řešení: zásilky se berou podle výnosu na kapacitu a z první, která se už nevejde, se vezme jen část. Žádný skutečný plán ho nepřekoná, takže je to horní mez pro každé `s` a nejnižší z nich je nejtěsnější. (Odborně: surogátní relaxace kapacitních omezení a Dantzigova mez.)

Složitost je O(k · n log n), kde k ≤ 32 je počet zkoušených hodnot `s` (každá znamená jedno seřazení zásilek), plus O(n · počet dodávek) při nakládání.

## Výsledky

Zadání neobsahuje data, proto zásilky generuji (pevný seed, log-normální rozdělení: většina zásilek je malá a levná, jen některé jsou velké nebo drahé). Scénáře:

- **běžné zásilky** – medián 8 l, 180 kg/m³ a 120 Kč, výnos nezávisí na velikosti; omezuje objem,
- **slabý den** – 60 000 běžných zásilek, vejdou se všechny,
- **těžké zboží** – 900 kg/m³; omezuje hmotnost,
- **smíšené zboží** – 800 kg/m³; omezuje hmotnost i objem zároveň,
- **výnos úměrný velikosti** – výnos na m³ je u všech zásilek podobný, takže se těžko vybírá.

| Scénář | Zásilek | Čas | Naloženo | Výnos (Kč) | Horní mez (Kč) | Mezera | `s` | Objem | Hmotnost |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Běžné zásilky | 300 000 | 0,68 s | 134 602 | 38 028 904 | 38 029 614 | 0,002 % | 0,00 | 100 % | 26 % |
| Slabý den | 60 000 | 0,18 s | 60 000 | 10 752 131 | 10 752 131 | 0 % | – | 79 % | 21 % |
| Těžké zboží | 300 000 | 0,54 s | 127 169 | 36 162 666 | 36 163 252 | 0,002 % | 1,00 | 95,5 % | 100 % |
| Smíšené zboží | 300 000 | 0,57 s | 133 468 | 37 621 329 | 37 624 706 | 0,009 % | 0,57 | 100 % | 100 % |
| Výnos úměrný velikosti | 300 000 | 0,54 s | 76 374 | 18 601 650 | 18 602 008 | 0,002 % | 0,00 | 100 % | 26 % |
| Běžné zásilky | 1 000 000 | 1,7 s | 184 504 | 73 780 397 | 73 781 277 | 0,001 % | 0,00 | 100 % | 26 % |

*Mezera* = o kolik je výnos nižší než horní mez; skutečná ztráta proti optimu je nejvýš taková. *Objem/Hmotnost* = využití kapacity flotily. Časy jsou změřené v Release na mém počítači; výnosy jsou díky pevnému seedu na každém počítači stejné.

**Srovnání s jednoduššími pravidly** – mezera k horní mezi podle pořadí nakládky (všechna pravidla nakládají stejným `GreedyPlanner`, liší se jen pořadím zásilek):

| Scénář | Adaptivní `s` | Pevné `s` = 0,5 | Výnos / m³ | Výnos / kg | Jen výnos |
|---|---:|---:|---:|---:|---:|
| Běžné zásilky | 0,002 % | 0,21 % | 0,002 % | 4,6 % | 15,8 % |
| Slabý den | 0 % | 0 % | 0 % | 0 % | 0 % |
| Těžké zboží | 0,002 % | 0,68 % | 3,5 % | 0,002 % | 19,7 % |
| Smíšené zboží | 0,009 % | 0,20 % | 2,7 % | 2,0 % | 18,2 % |
| Výnos úměrný velikosti | 0,002 % | 1,46 % | 0,002 % | 14,8 % | 19,2 % |
| Běžné zásilky, 1 mil. | 0,001 % | 0,38 % | 0,001 % | 8,7 % | 27,5 % |

Co z toho plyne:

- **Každé pevné pravidlo v některém scénáři selže.** Výnos na m³ je výborný, když omezuje objem, ale u těžkého zboží ztrácí 3,5 %; výnos na kg naopak. Adaptivní volba `s` je všude do 0,01 % od horní meze.
- **Naivní „nejdražší napřed“ přichází o 16–27 % výnosu.**
- **Na slabém dni na pořadí nezáleží** – odjede všechno.
- **Na volbě dodávky záleží.** S prostým „první dodávka, kam se zásilka vejde“ (first-fit) byla mezera u těžkého zboží 0,40 % a u smíšeného 0,57 %: první dodávky se zaplnily objemnými lehkými zásilkami a jejich nosnost zůstala nevyužitá. Pravidlo skalárního součinu ji snížilo na 0,002 % a 0,009 % za cenu několika desítek milisekund na 300 000 zásilek.
- **S čísly ze zadání je poptávka mnohonásobně vyšší než kapacita.** 300 000 běžných zásilek má asi 3 300 m³, tedy zhruba čtyřnásobek kapacity okruhu. Odjede 45 % zásilek, které ale nesou 70 % výnosu.
- **Kde je hladový přístup slabý:** v testech na malých případech (2 dodávky, 12 zásilek, do dodávky se vejdou jen 2–4) dosahuje proti přesnému optimu z hrubé síly 93 % celkem a v nejhorším případě 72,5 %. Jedna špatně zvolená velká zásilka tam zablokuje velkou část dodávky. U skutečných zásilek, kterých se do dodávky vejdou stovky, se to neprojeví.

Lokální prohledávání (výměny zásilek mezi dodávkami a skladem) jsem zvažoval jako další krok, ale při změřené mezeře do 0,01 % by nic podstatného nepřineslo.

## Předpoklady a zjednodušení

- Každá zásilka může jet kteroukoli dodávkou; přiřazení k trasám a AlzaBoxům řeší jiná část logistiky. Kdyby měla každá dodávka vlastní okruh, stačí plánovat zvlášť pro zásilky každého okruhu.
- Počítám jen hmotnost a objem – ne rozměry, stohování ani kapacitu schránek v AlzaBoxu.
- Výnos, hmotnost i objem jsou kladné a výnos nezávisí na tom, kterou dodávkou ani kterou jízdou zásilka pojede.
- Flotila je pevná: všech 120 dodávek jede vždy, takže na slabém dni nešetřím počtem dodávek.
- Výnos je v kódu `double`, ne `decimal`: je to odhad pro plánování, ne účetní částka, a plánovač s ním dělá miliony operací.

## Dvě jízdy denně

- Plánovač plánuje jednu jízdu všech dodávek. Zásilky, které nenaložil (`DeliveryPlan.LeftOver`), přejdou spolu s nově došlými do dalšího plánování.
- Pokud výnos zásilky nezávisí na tom, kterou jízdou pojede, dá plánování po jízdách stejný denní výnos jako plánování celého dne najednou: první jízda vezme nejvýhodnější zásilky, druhá další v pořadí.
- Plánování po jízdách má navíc výhody: nejcennější zásilky odjedou dřív, druhá jízda počítá i se zásilkami došlými mezi jízdami a `s` se pro ni znovu naladí podle toho, co ve skladu zbylo.

## Rizika a možná rozšíření

- **Levné zásilky mohou čekat.** Maximalizace výnosu každé jízdy odsouvá zásilky s nízkým výnosem; sklad vyprázdní až slabé dny (úterý, čtvrtek), takže zásilka může čekat i několik dní. Řešením je přičíst k výnosu bonus za dobu čekání (nebo penále za zpoždění) – plánovač zůstane stejný, změní se jen priorita.
- **Velké zásilky vůči dodávce:** doplnit lokální prohledávání nebo přesný solver pro poslední část nakládky.
- **Rychlost:** hledat `s` zlatým řezem místo jemné mřížky, počítat horní mez výběrem v O(n) místo řazení a na slabém dni hledání úplně přeskočit.
- **Slabé dny:** minimalizovat počet dodávek, které musí vyjet.
- **Reálná data:** nakalibrovat generátor (a testy) podle skutečných zásilek.

## Zvažované alternativy

- **Přesné řešení celočíselným programováním** (např. OR-Tools, Gurobi): 300 000 zásilek × 120 dodávek = 36 milionů binárních proměnných, v krátkém časovém okně nereálné. U takto malých zásilek by navíc mohlo zlepšit výnos nejvýš o mezeru do 0,01 %.
- **Dynamické programování:** je pseudopolynomiální v kapacitě; se dvěma omezeními a 120 dodávkami je stavový prostor obrovský.
- **Metaheuristiky** (genetické algoritmy, simulované žíhání, tabu search): na statisících zásilek pomalu konvergují, nejsou deterministické a potřebují ladit parametry. Dávají smysl jen při velké mezeře k optimu, a ta tu není.

## Struktura řešení

| Soubor | Účel |
|---|---|
| `Alza.Planner/AdaptiveGreedyPlanner.cs` | hlavní plánovač: zvolí `s` a vrátí plán s horní mezí |
| `Alza.Planner/GreedyPlanner.cs` | hladové nakládání podle zadané priority, slabý den, volba dodávky |
| `Alza.Planner/RevenueUpperBound.cs` | horní mez výnosu a hledání nejtěsnějšího `s` |
| `Alza.Planner/ParcelPriority.cs` | priority: výnos na kapacitu a jednoduchá pravidla pro srovnání |
| `Alza.Planner/PlanValidator.cs` | nezávislá kontrola plánu: žádná přetížená dodávka, každá zásilka právě jednou |
| `Alza.Planner/Models/` | `Parcel`, `Fleet`, `VanLoad`, `DeliveryPlan` |
| `Alza.Planner/Generation/` | generátor zásilek a profily scénářů |
| `Alza/Program.cs` | konzolová aplikace: scénáře, časy, srovnání |
| `Alza.Planner.Tests/` | testy (xUnit) |

Testy ověřují, že žádný plán nepřetíží dodávku a každou zásilku naplánuje právě jednou (pro všechna pravidla a všechny scénáře), že výnos nikdy nepřekročí horní mez, že na realistických datech zůstane plán do 0,1 % od horní meze, že horní mez není nikdy pod přesným optimem z hrubé síly, že na slabém dni odjede všechno a že plánovač dává ručně spočítaný výsledek na malém příkladu. Zvlášť testují i samotný validátor, aby ostatní testy neprocházely naprázdno.
