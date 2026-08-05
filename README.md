# Profilisanje i optimizacija memorijske i procesorske efikasnosti C# aplikacije

Eksperimentalna .NET platforma razvijena za potrebe master rada. Projekat ispituje kako različite C# i CLR implementacione strategije utiču na vreme izvršavanja, managed alokacije, garbage collection i ponašanje sistema pod ravnomernim i burst opterećenjem.

Sistem obuhvata deterministički generator logova, kontrolisane SQL Server datasetove, READ/WRITE putanje, Resolver, BenchmarkDotNet eksperimente H1–H10 i namenski load test.

## Struktura sistema

| Projekat | Namena |
|---|---|
| `Domain` | Modeli i kanonski format log zapisa |
| `Generator` | Kontrolisano i ponovljivo generisanje logova |
| `Infrastructure` | SQL pristup, batch buffer i `SqlBulkCopy` |
| `DatasetLoader` | Generisanje, upis i validacija datasetova |
| `DatasetReader` | Streaming čitanje i povezivanje sa Resolverom |
| `Resolver` | Split, Span, sekvencijalna i paralelna obrada |
| `Benchmarks` | BenchmarkDotNet eksperimenti |
| `LoadTests` | Steady/burst test za H9 |
| `Resolver.Tests`, `Infrastructure.Tests` | Funkcionalni testovi |

```text
Generator -> batch buffer -> SqlBulkCopy -> SQL Server
SQL Server -> SqlDataReader -> Resolver -> checksum validacija
```

Generator koristi fiksni seed, pa isti profil i broj zapisa uvek proizvode isti skup podataka. Checksum se proverava nakon upisa, čitanja i obrade.

## Preduslovi

- .NET SDK 9;
- Docker Desktop;
- DBeaver ili drugi SQL klijent;
- najmanje 10 GB slobodnog Docker prostora za sve datasetove.

## Brzo pokretanje

Kreirati lokalnu konfiguraciju i u `.env` uneti jaku SA lozinku:

```bash
cp .env.example .env
```

```env
MSSQL_SA_PASSWORD='ReplaceWithYourStrongPassword123!'
```

Pokrenuti SQL Server:

```bash
docker compose up -d
```

U izabrani Database client, ja sam koristio Dbeaver se povezati na `localhost:1433` kao korisnik `sa`, uključiti **Trust server certificate** i redom izvršiti:

1. `sql/001-create-database.sql` nad bazom `master`;
2. `sql/002-schema.sql`;
3. sledeću naredbu pre velikih importa:

```sql
ALTER DATABASE OptimizationResearch SET RECOVERY SIMPLE;
```

U terminalu postaviti konekcioni string:

```bash
set -a
source .env
set +a

export OPTIMIZATION_SQL_CONNECTION_STRING="Server=localhost,1433;Database=OptimizationResearch;User Id=sa;Password=${MSSQL_SA_PASSWORD};Encrypt=True;TrustServerCertificate=True"
```

Zatim izgraditi i proveriti rešenje:

```bash
dotnet build Optimization.sln -c Release
dotnet test Optimization.sln -c Release --no-build
```

## Kontrolisani datasetovi

Loader prihvata profil, broj redova, seed i opcionu veličinu batch-a:

```text
DatasetLoader <Standard|ErrorHeavy> <broj-redova> <seed> [batch-capacity]
```

Završni eksperimenti koriste profile `Standard` i `ErrorHeavy`, veličine 5.000, 100.000 i 1.000.000, seed `12345` i batch od 5.000 redova. Primeri:

```bash
dotnet run --no-restore -c Release --project DatasetLoader -- Standard 100000 12345 5000
dotnet run --no-restore -c Release --project DatasetLoader -- ErrorHeavy 1000000 12345 5000
```

Čitanje i Resolver validacija:

```bash
dotnet run --no-restore -c Release --project DatasetReader -- 1 span
dotnet run --no-restore -c Release --project DatasetReader -- 1 split
dotnet run --no-restore -c Release --project DatasetReader -- 6 span
```

## Pokretanje eksperimenata

Lista dostupnih benchmarkova:

```bash
dotnet run -c Release --project Benchmarks -- --list flat
```

Primer za H1:

```bash
dotnet run --no-restore -c Release --project Benchmarks -- \
  --filter "Benchmarks.ResolverBenchmarks.*"
```

Kompletna validacija baze i izvršavanje svih eksperimenata:

```bash
chmod +x scripts/run-experiments.sh
./scripts/run-experiments.sh
```

Na macOS-u se dugo izvršavanje može zaštititi od uspavljivanja:

```bash
caffeinate -dimsu ./scripts/run-experiments.sh
```

Svako izvršavanje dobija poseban direktorijum u `docs/raw-results/`.

## Sažetak rezultata

| Hipoteza | Poređenje | Glavni nalaz |
|---|---|---|
| H1 | `Span<T>` / `string.Split` | Span je 25–33% brži i alocira 23–24% manje |
| H2 | Typed lista / boxing | Boxing alocira 75% više i do 4,17 puta je sporiji |
| H3 | Sequential / `Parallel.For` / PLINQ | Paralelizacija pomaže, ali najbolji pristup zavisi od veličine skupa |
| H4 | Obrada izuzetaka | `try/catch` po elementu je 31–35% sporiji; bacanje je 8–65 puta sporije |
| H5 | Redosled polja u struct-u | Smanjuje veličinu strukture, ali ovde nije ubrzao obradu |
| H6 | `stackalloc` / heap | `stackalloc` je oko 60% brži za mali lokalni bafer |
| H7 | Metoda / delegati / lambda | Direktan poziv je najbrži; capturing lambda pri kreiranju alocira 88 B |
| H8 | `class` / `struct` | Struct je 5,6–6,3 puta brži u kontrolisanom scenariju |
| H9 | Steady / burst | Burst ima 8,73 puta veći red i 6,24 puta veću prosečnu latenciju |
| H10 | Allocation / reuse | Reuse je oko 4,1 puta brži i uklanja alokaciju bafera |

Detaljne tabele, tumačenje i ograničenja nalaze se u [centralnom dokumentu rezultata](./docs/central-results-hypothesis-1-to-10.md). Završni sirovi izlazi nalaze se u [`docs/raw-results/20260731-002131/`](./docs/raw-results/20260731-002131/).

## Metodološke napomene

- SQL I/O za H1, H2, H3 i encoding obavlja se u `GlobalSetup` fazi i nije deo izmerenog vremena.
- BenchmarkDotNet `Allocated` predstavlja managed memoriju po operaciji, a GC kolone su normalizovane na 1.000 operacija.
- H9 je vremenski load test, a ne mikrobenchmark.
- Rezultati zavise od hardvera, operativnog sistema i verzije .NET runtime-a.
- Nalazi važe za definisane eksperimentalne uslove i nisu univerzalne preporuke.

## Dijagrami

![Sekvencijalni dijagram generisanja logova](./pngs/Sekvencijalni%20dijagram%20generisanja%20logova.png)

Završni komponentni, WRITE, READ, ER i eksperimentalni dijagrami biće dodati nakon pojedinačne provere.

## Zaustavljanje okruženja

```bash
docker compose down
```

Komanda `docker compose down -v` dodatno briše SQL volume i sve generisane datasetove.



