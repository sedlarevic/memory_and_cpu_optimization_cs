IF DB_ID(N'OptimizationResearch') IS NULL
BEGIN
    EXEC
    (
        N'CREATE DATABASE [OptimizationResearch];'
    );
END;
