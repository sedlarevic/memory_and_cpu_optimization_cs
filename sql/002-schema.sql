USE OptimizationResearch;

IF OBJECT_ID(N'dbo.Datasets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Datasets
    (
        DatasetId BIGINT IDENTITY(1, 1) NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        GenerationProfile VARCHAR(30) NOT NULL,
        Seed INT NOT NULL,
        TargetCount INT NOT NULL,

        CreatedAtUtc DATETIME2(7) NOT NULL
            CONSTRAINT DF_Datasets_CreatedAtUtc
            DEFAULT SYSUTCDATETIME(),

        Description NVARCHAR(500) NULL,

        CONSTRAINT PK_Datasets
            PRIMARY KEY CLUSTERED (DatasetId),

        CONSTRAINT UQ_Datasets_Name
            UNIQUE (Name),

        CONSTRAINT CK_Datasets_TargetCount
            CHECK (TargetCount > 0),

        CONSTRAINT CK_Datasets_GenerationProfile
            CHECK
            (
                GenerationProfile IN
                (
                    'Standard',
                    'ErrorHeavy'
                )
            )
    );
END;

IF OBJECT_ID(N'dbo.LogEntries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LogEntries
    (
        DatasetId BIGINT NOT NULL,
        LogIndex INT NOT NULL,
        FromState TINYINT NOT NULL,
        ToState TINYINT NOT NULL,
        Level VARCHAR(9) NOT NULL,
        Message NVARCHAR(4000) NOT NULL,

        CONSTRAINT PK_LogEntries
            PRIMARY KEY CLUSTERED
            (
                DatasetId,
                LogIndex
            ),

        CONSTRAINT FK_LogEntries_Datasets
            FOREIGN KEY (DatasetId)
            REFERENCES dbo.Datasets (DatasetId)
            ON DELETE CASCADE,

        CONSTRAINT CK_LogEntries_LogIndex
            CHECK (LogIndex > 0),

        CONSTRAINT CK_LogEntries_FromState
            CHECK (FromState BETWEEN 0 AND 5),

        CONSTRAINT CK_LogEntries_ToState
            CHECK (ToState BETWEEN 0 AND 5),

        CONSTRAINT CK_LogEntries_Level
            CHECK
            (
                Level IN
                (
                    '[INFO]',
                    '[WARNING]',
                    '[ERROR]'
                )
            )
    );
END;
