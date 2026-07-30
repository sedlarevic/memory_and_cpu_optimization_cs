# Centralni rezultati eksperimenata H1–H10

## Eksperimentalno okruženje

| Stavka | Vrednost |
|---|---|
| Operativni sistem | macOS Sequoia 15.6.1 |
| Procesor | Apple M1, 8 fizičkih i 8 logičkih jezgara |
| Memorija | 8 GB |
| .NET SDK | 9.0.302 |
| Runtime | .NET 9.0.7, Arm64 RyuJIT |
| Benchmark alat | BenchmarkDotNet 0.15.8 |
| Baza | SQL Server 2025 Developer u Docker kontejneru |
| Završno izvršavanje | `20260731-002131` |

Za H1, H2, H3 i dodatni encoding eksperiment korišćeni su
kontrolisani `Standard` datasetovi iz SQL Server baze. SQL čitanje i
priprema ulaza obavljeni su u `GlobalSetup` fazi i nisu uključeni u
izmereno vreme benchmark metoda.

H9 je izvršen kao namenski load test: jedno warmup izvršavanje i pet
merenih izvršavanja od po deset sekundi za svaki obrazac opterećenja.

## Centralna tabela

| Hipoteza | Predmet poređenja | Glavne klase | Konačni rezultat | Status |
|---|---|---|---|---|
| H1 | `Span<T>` naspram `string.Split` | `ResolverBenchmarks`, `LogResolver` | `Span` je 25–33% brži i alocira 23–24% manje memorije | Potvrđena |
| H2 | Typed lista naspram `List<object>` sa boxingom | `BoxingBenchmarks`, `LogEntryValue` | Boxing alocira 75% više memorije i na većim skupovima je 3,02–4,17 puta sporiji | Potvrđena |
| H3 | Sekvencijalno, `Parallel.For` i PLINQ | `ParallelResolverBenchmarks`, `LogBatchResolver` | Paralelne varijante su brže; najbolja strategija zavisi od veličine skupa | Delimično potvrđena |
| H4 | Pozicija `try/catch` bloka i bacanje izuzetaka | `ExceptionHandlingBenchmarks`, `ThrowingExceptionBenchmarks` | Blok oko petlje ima zanemarljiv trošak; blok po elementu je 31–35% sporiji; bacanje izuzetaka je 8–65 puta sporije | Potvrđena uz preciziranje |
| H5 | Redosled polja i poravnanje `struct` tipova | `StructAlignmentBenchmarks` | Veličina je smanjena sa približno 32 B na 16 B, ali obrada nije ubrzana | Delimično potvrđena |
| H6 | `stackalloc byte[256]` naspram `new byte[256]` | `StackHeapBenchmarks` | `stackalloc` je približno 60% brži i nema managed alokacije | Potvrđena za male lokalne bafere |
| H7 | Direktna metoda, delegati i lambda izrazi | `LambdaBenchmarks`, `LambdaCreationBenchmarks` | Direktan poziv je najbrži; method-group delegat je oko 2,5 puta sporiji; capturing lambda pri kreiranju alocira 88 B | Potvrđena uz preciziranje |
| H8 | `class` naspram `struct` zapisa | `ClassStructGenerationBenchmarks` | `struct` je približno 5,6–6,3 puta brži; klasa alocira 48 B po zapisu | Potvrđena u kontrolisanom scenariju |
| H9 | Steady naspram burst opterećenja | `BurstSteadyLoadTest`, `LoadTestResult` | Burst pravi 8,73 puta veći red, 6,24 puta veću prosečnu latenciju i znatno više Gen2 kolekcija | Potvrđena |
| H10 | Novi bafer naspram ponovne upotrebe | `AllocationReuseBenchmarks` | Reuse je približno 4,1 puta brži i eliminiše 1.048 B alokacije po zapisu | Potvrđena za sinhronu obradu |

## H1 — `Span<T>` naspram `string.Split`

| Broj redova | Split | Span | Span odnos | Split memorija | Span memorija |
|---:|---:|---:|---:|---:|---:|
| 5.000 | 949,2 µs | 635,4 µs | 0,67 | 3,79 MB | 2,93 MB |
| 100.000 | 19.038,5 µs | 14.286,4 µs | 0,75 | 74,83 MB | 57,69 MB |
| 1.000.000 | 191.283,3 µs | 132.281,7 µs | 0,69 | 753,70 MB | 575,43 MB |

Kod milion redova `Span` je smanjio vreme za približno 59,00 ms i
managed alokacije za približno 178,27 MB. Gen0 vrednosti su smanjene
sa 125.666,67 na 96.000 normalizovanih kolekcija na 1.000 operacija.

## H2 — Typed lista naspram boxing/unboxing pristupa

| Broj elemenata | Typed lista | Object lista | Odnos | Typed memorija | Object memorija |
|---:|---:|---:|---:|---:|---:|
| 5.000 | 63,60 µs | 78,72 µs | 1,24 | 156,35 KB | 273,49 KB |
| 100.000 | 2.243,45 µs | 9.365,46 µs | 4,17 | 3.125,12 KB | 5.468,90 KB |
| 1.000.000 | 24.182,53 µs | 72.890,66 µs | 3,02 | 31.250,11 KB | 54.687,55 KB |

Boxing varijanta je u sve tri veličine alocirala 1,75 puta više
memorije. Na većim skupovima dodatne alokacije su izazvale znatno
veći GC pritisak.

## H3 — Sekvencijalna i paralelna obrada

| Broj redova | Sequential | `Parallel.For` | PLINQ | Najbrže |
|---:|---:|---:|---:|---|
| 5.000 | 625,8 µs | 411,7 µs | 455,2 µs | `Parallel.For` |
| 100.000 | 13.130,9 µs | 10.017,6 µs | 8.268,9 µs | PLINQ |
| 1.000.000 | 137.966,0 µs | 90.024,7 µs | 78.474,1 µs | PLINQ |

`Parallel.For` je bio najbolji na malom skupu, a PLINQ na srednjem i
velikom skupu. Paralelne strategije koriste ThreadPool i pokazuju veću
varijabilnost od sekvencijalne obrade, pa zaključak nije univerzalna
preporuka za svako opterećenje.

## H4 — Obrada izuzetaka

### H4a — `try/catch` bez bacanja izuzetka

| Iteracije | Bez `try/catch` | Blok oko petlje | Blok po elementu |
|---:|---:|---:|---:|
| 5.000 | 5,022 µs | 5,113 µs | 6,576 µs |
| 100.000 | 98,469 µs | 98,520 µs | 131,351 µs |
| 1.000.000 | 969,438 µs | 983,141 µs | 1.304,331 µs |

Jedan blok oko cele petlje imao je zanemarljiv trošak. Postavljanje
bloka u svaku iteraciju povećalo je vreme približno 31–35%.

### H4b — Povratni kod naspram stvarnog izuzetka

| Operacije | Greška na svakih | Povratni kod | Throw/catch | Odnos |
|---:|---:|---:|---:|---:|
| 10.000 | 100 | 16,08 µs | 1.031,87 µs | 64,19 |
| 10.000 | 1.000 | 15,35 µs | 125,19 µs | 8,16 |
| 100.000 | 100 | 159,35 µs | 10.287,48 µs | 64,56 |
| 100.000 | 1.000 | 152,71 µs | 1.219,52 µs | 7,99 |

Stvarno bacanje i hvatanje izuzetka alocira približno 320 B po
izuzetku. Trošak ukupnog scenarija direktno zavisi od učestalosti
greške.

## H5 — Struct alignment i redosled polja

| Broj elemenata | Loš raspored | Optimizovan raspored | Odnos |
|---:|---:|---:|---:|
| 5.000 | 14,36 µs | 14,33 µs | 1,00 |
| 100.000 | 287,45 µs | 287,61 µs | 1,00 |
| 1.000.000 | 2.886,39 µs | 2.882,23 µs | 1,00 |

Redosled polja smanjio je veličinu strukture sa približno 32 B na
16 B, ali nije proizveo merljivo ubrzanje korišćene sekvencijalne
petlje.

## H6 — Stack naspram heap alokacije

| Iteracije | Heap | Stack | Stack odnos | Heap alokacija | Stack alokacija |
|---:|---:|---:|---:|---:|---:|
| 5.000 | 95,03 µs | 37,26 µs | 0,39 | 1.400.000 B | 0 B |
| 100.000 | 1.881,48 µs | 756,62 µs | 0,40 | 28.000.000 B | 0 B |
| 1.000.000 | 19.145,46 µs | 7.557,18 µs | 0,39 | 280.000.000 B | 0 B |

Rezultat važi za mali bafer od 256 B čiji je životni vek ograničen na
jednu iteraciju. Ne predstavlja preporuku za velike stack alokacije.

## H7 — Metode, delegati i lambda izrazi

### H7a — Trošak pozivanja

| Iteracije | Direktna metoda | Method group | Non-capturing | Capturing |
|---:|---:|---:|---:|---:|
| 5.000 | 3,149 µs | 7,986 µs | 3,613 µs | 3,327 µs |
| 100.000 | 63,900 µs | 160,235 µs | 72,393 µs | 66,834 µs |
| 1.000.000 | 638,806 µs | 1.601,735 µs | 720,715 µs | 669,033 µs |

Direktan poziv je bio najbrži. Method-group delegat bio je približno
2,51–2,54 puta sporiji, dok su lambda varijante bile 5–15% sporije od
direktnog poziva.

### H7b — Trošak kreiranja

| Varijanta | Vreme | Alokacija |
|---|---:|---:|
| Static method group | praktično 0 ns | 0 B |
| Non-capturing lambda | praktično 0 ns | 0 B |
| Capturing lambda | 8,7464 ns | 88 B |

Vrednosti bliske nuli bile su neodvojive od trajanja prazne benchmark
metode. Capturing lambda zahteva closure objekat i pravi managed
alokaciju.

## H8 — Class naspram struct zapisa

| Broj zapisa | Class | Struct | Struct odnos | Class alokacija | Struct alokacija |
|---:|---:|---:|---:|---:|---:|
| 5.000 | 27,756 µs | 4,773 µs | 0,17 | 240.000 B | 0 B |
| 100.000 | 544,161 µs | 96,689 µs | 0,18 | 4.800.000 B | 0 B |
| 1.000.000 | 5.874,904 µs | 964,311 µs | 0,16 | 48.000.000 B | 0 B |

Klasa je alocirala 48 B po zapisu. Rezultat potvrđuje korist vrednosnog
tipa u ovom uskom scenariju stvaranja kratkotrajnih zapisa, ali ne znači
da je `struct` univerzalna zamena za klasu.

## H9 — Steady naspram burst opterećenja

Oba testa su obradila milion zapisa tokom približno deset sekundi.

| Metrika | Steady | Burst | Odnos ili razlika |
|---|---:|---:|---:|
| Throughput | 99.975,506 zapisa/s | 99.949,047 zapisa/s | praktično jednako |
| CPU vreme | 2.956,727 ms | 1.884,716 ms | burst nije povećao CPU vreme |
| Alocirano | 967,900 MB | 971,676 MB | +0,39% |
| Gen0 | 163,4 | 178,6 | +9,3% |
| Gen1 | 78,4 | 69,0 | −12,0% |
| Gen2 | 2,4 | 20,2 | 8,42 puta više |
| Maksimalni red | 7.823,4 | 68.277,4 | 8,73 puta veći |
| Prosečna latencija | 6,808 ms | 42,499 ms | 6,24 puta veća |
| P95 latencija | 12,445 ms | 58,622 ms | 4,71 puta veća |
| P99 latencija | 13,948 ms | 64,002 ms | 4,59 puta veća |
| Throughput CV | 0,000 | 2,127 | izrazito promenljiv dolazni tok |

Throughput je namerno gotovo jednak jer oba scenarija imaju isti
prosečan rate i ukupan broj zapisa. Negativan efekat burst režima vidi
se u dubini reda, repnim latencijama i Gen2 aktivnosti.

## H10 — Allocation-heavy naspram reuse-heavy pristupa

| Broj zapisa | Allocation-heavy | Reuse-heavy | Reuse odnos | Allocation memorija | Reuse memorija |
|---:|---:|---:|---:|---:|---:|
| 5.000 | 258,98 µs | 62,27 µs | 0,24 | 5.240.000 B | 0 B |
| 100.000 | 5.239,74 µs | 1.246,54 µs | 0,24 | 104.800.000 B | 0 B |
| 1.000.000 | 51.458,82 µs | 12.453,65 µs | 0,24 | 1.048.000.000 B | 0 B |

Allocation-heavy pristup pravi 1.048 B novih managed alokacija po
zapisu. Ponovna upotreba je približno 4,1 puta brža. Zaključak važi za
sinhronu obradu u kojoj bafer nije istovremeno deljen između niti.

## Dodatni eksperiment — UTF-8 naspram UTF-16

| Broj redova | UTF-8 | UTF-16 | UTF-16 odnos | Alokacija obe varijante |
|---:|---:|---:|---:|---:|
| 5.000 | 88,88 µs | 118,58 µs | 1,33 | 553,51 KB |
| 100.000 | 1.148,38 µs | 1.733,38 µs | 1,51 | 11.313,98 KB |
| 1.000.000 | 13.857,32 µs | 19.312,35 µs | 1,39 | 115.123,22 KB |

Za kontrolisani pretežno ASCII sadržaj iz SQL dataseta UTF-8
dekodiranje bilo je približno 25–34% brže posmatrano u odnosu na vreme
UTF-16 varijante. Obe metode proizvode isti .NET string, pa je izlazna
managed memorija jednaka.

## Dodatni eksperiment — Standard naspram ErrorHeavy profila

| Broj zapisa | Standard vreme | ErrorHeavy vreme | Standard memorija | ErrorHeavy memorija |
|---:|---:|---:|---:|---:|
| 5.000 | 704,6 µs | 1.296,6 µs | 4,95 MB | 15,07 MB |
| 100.000 | 13.786,4 µs | 26.393,1 µs | 97,07 MB | 312,52 MB |
| 1.000.000 | 140.556,8 µs | 262.665,7 µs | 967,64 MB | 3.080,27 MB |

`ErrorHeavy` profil bio je 1,84–1,91 puta sporiji i alocirao
3,04–3,22 puta više managed memorije. Ovo je sadržajni profil
generatora i nije vremenski H9 burst test.

## Validacija završnog izvršavanja

- Dataset 1: 5.000 redova, validacija prošla.
- Dataset 6: 1.000.000 redova, validacija prošla.
- Svi BenchmarkDotNet procesi završeni su oznakom `BenchmarkRunner: End`.
- H9 steady i burst imaju podudarne checksum vrednosti.
- H9 steady i burst obradili su očekivani broj zapisa.

Sirovi rezultati nalaze se u:

```text
docs/raw-results/20260731-002131/
```

## Metodološke napomene

- `Mean` je aritmetička sredina BenchmarkDotNet merenja.
- `Allocated` predstavlja managed memoriju po jednoj benchmark operaciji.
- BenchmarkDotNet GC kolone su normalizovane na 1.000 operacija.
- SQL I/O za H1, H2, H3 i encoding nije deo izmerenog vremena.
- H9 nije mikrobenchmark i koristi namenski vremenski runner.
- Rezultati važe za navedeno hardversko i softversko okruženje i ne
  predstavljaju univerzalne konstante za sve .NET platforme.
