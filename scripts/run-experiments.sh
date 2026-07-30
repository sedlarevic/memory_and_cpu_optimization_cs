#!/usr/bin/env bash

set -euo pipefail

ROOT_DIRECTORY="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT_DIRECTORY"

TIMESTAMP="$(date +%Y%m%d-%H%M%S)"

RESULTS_DIRECTORY="$ROOT_DIRECTORY/docs/raw-results/$TIMESTAMP"
BENCHMARK_DIRECTORY="$RESULTS_DIRECTORY/benchmarkdotnet"

mkdir -p "$BENCHMARK_DIRECTORY"

if [[ ! -f ".env" ]]; then
    echo "Missing .env file."
    exit 1
fi

set -a
source .env
set +a

if [[ -z "${MSSQL_SA_PASSWORD:-}" ]]; then
    echo "MSSQL_SA_PASSWORD is not configured."
    exit 1
fi

export OPTIMIZATION_SQL_CONNECTION_STRING="Server=localhost,1433;Database=OptimizationResearch;User Id=sa;Password=${MSSQL_SA_PASSWORD};Encrypt=True;TrustServerCertificate=True"

run_benchmark()
{
    local result_name="$1"
    local benchmark_filter="$2"

    echo
    echo "========================================"
    echo "Running $result_name"
    echo "========================================"

    dotnet run \
        --no-restore \
        -c Release \
        --project Benchmarks -- \
        --filter "$benchmark_filter" \
        --artifacts "$BENCHMARK_DIRECTORY" \
        2>&1 |
        tee "$RESULTS_DIRECTORY/$result_name-console.txt"
}

echo "Starting SQL Server..."

if docker container inspect sql2025 > /dev/null 2>&1
then
    if [[ "$(docker inspect \
        --format '{{.State.Running}}' \
        sql2025)" != "true" ]]
    then
        docker start sql2025 > /dev/null
    fi

    echo "Existing sql2025 container is running."
else
    docker compose up -d
fi

echo "Waiting for SQL Server..."

sql_ready=false

for attempt in {1..30}
do
    if docker exec sql2025 \
        /opt/mssql-tools18/bin/sqlcmd \
        -S localhost \
        -U sa \
        -P "$MSSQL_SA_PASSWORD" \
        -C \
        -Q "SELECT 1;" \
        > /dev/null 2>&1
    then
        sql_ready=true
        break
    fi

    sleep 2
done

if [[ "$sql_ready" != "true" ]]
then
    echo "SQL Server did not become ready."
    exit 1
fi

echo "SQL Server is ready."

echo
echo "Building solution..."
dotnet build Optimization.sln -c Release

echo
echo "Running tests..."
dotnet test \
    Optimization.sln \
    -c Release \
    --no-build

echo
echo "Validating SQL datasets..."

dotnet run \
    --no-restore \
    -c Release \
    --project DatasetReader -- \
    1 span \
    2>&1 |
    tee "$RESULTS_DIRECTORY/dataset-1-validation.txt"

dotnet run \
    --no-restore \
    -c Release \
    --project DatasetReader -- \
    6 span \
    2>&1 |
    tee "$RESULTS_DIRECTORY/dataset-6-validation.txt"

# H1
run_benchmark \
    "H1-resolver" \
    "Benchmarks.ResolverBenchmarks.*"

# H2
run_benchmark \
    "H2-boxing" \
    "Benchmarks.BoxingBenchmarks.*"

# H3
run_benchmark \
    "H3-parallel" \
    "Benchmarks.ParallelResolverBenchmarks.*"

# H4a
run_benchmark \
    "H4a-exception-position" \
    "Benchmarks.ExceptionHandlingBenchmarks.*"

# H4b
run_benchmark \
    "H4b-throwing" \
    "Benchmarks.ThrowingExceptionBenchmarks.*"

# H5
run_benchmark \
    "H5-struct-alignment" \
    "Benchmarks.StructAlignmentBenchmarks.*"

# H6
run_benchmark \
    "H6-stack-heap" \
    "Benchmarks.StackHeapBenchmarks.*"

# H7a
run_benchmark \
    "H7a-lambda-invocation" \
    "Benchmarks.LambdaBenchmarks.*"

# H7b
run_benchmark \
    "H7b-lambda-creation" \
    "Benchmarks.LambdaCreationBenchmarks.*"

# H8
run_benchmark \
    "H8-class-struct" \
    "Benchmarks.ClassStructGenerationBenchmarks.*"

echo
echo "========================================"
echo "Running H9 steady"
echo "========================================"

dotnet run \
    --no-restore \
    -c Release \
    --project LoadTests -- \
    steady 10 100000 5 \
    2>&1 |
    tee "$RESULTS_DIRECTORY/H9-steady.txt"

echo
echo "========================================"
echo "Running H9 burst"
echo "========================================"

dotnet run \
    --no-restore \
    -c Release \
    --project LoadTests -- \
    burst 10 100000 5 \
    2>&1 |
    tee "$RESULTS_DIRECTORY/H9-burst.txt"

# H10
run_benchmark \
    "H10-allocation-reuse" \
    "Benchmarks.AllocationReuseBenchmarks.*"

# Dodatni encoding eksperiment
run_benchmark \
    "Additional-encoding" \
    "Benchmarks.EncodingBenchmarks.*"

# Dodatni generator profile eksperiment
run_benchmark \
    "Additional-generator-profile" \
    "Benchmarks.GeneratorProfileBenchmarks.*"

echo
echo "========================================"
echo "All experiments completed successfully."
echo "Results: $RESULTS_DIRECTORY"
echo "========================================"
