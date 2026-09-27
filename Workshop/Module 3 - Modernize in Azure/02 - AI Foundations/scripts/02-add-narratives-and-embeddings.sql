/*
    Module 3 | Script 02 of 09 | Generate narrative embeddings
    Purpose: Create the embedding table and vectorize the first 100 loan narratives.
    Run after script 01, against your assigned ZavaLendingDB-No database.
*/

-- Fail early when script 01 has not completed.
IF NOT EXISTS (SELECT 1 FROM sys.external_models WHERE name = 'FoundryEmbeddingModel')
    THROW 50001, 'Required external model does not exist.', 1;

-- Step 2a: Create the embeddings table
PRINT '=== Creating LoanNarrativeEmbeddings table ==='
GO

-- float16 stores the model's 3,072 dimensions within the vector size limit.

IF OBJECT_ID('dbo.LoanNarrativeEmbeddings', 'U') IS NOT NULL
    DROP TABLE dbo.LoanNarrativeEmbeddings;
GO

CREATE TABLE dbo.LoanNarrativeEmbeddings
(
    LoanId              BIGINT                  NOT NULL PRIMARY KEY,
    NarrativeEmbedding  VECTOR(3072, float16)   NOT NULL,
    GeneratedAt         DATETIME2               NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_LoanNarrativeEmbeddings_LoanHistory
        FOREIGN KEY (LoanId) REFERENCES dbo.LoanHistory(LoanId)
);
GO

PRINT '  Table created.'
GO

-- Step 2b: Generate embeddings from LoanNarrative text
PRINT '=== Generating embeddings using AI_GENERATE_EMBEDDINGS ==='
GO

-- Make this step repeatable when the table definition is adapted for troubleshooting.
IF EXISTS (SELECT 1 FROM dbo.LoanNarrativeEmbeddings e WHERE e.LoanId BETWEEN 1 AND 100)
    DELETE FROM dbo.LoanNarrativeEmbeddings WHERE LoanId BETWEEN 1 AND 100;
GO

-- Use small batches to make endpoint progress visible and simplify retry diagnosis.

INSERT INTO dbo.LoanNarrativeEmbeddings (LoanId, NarrativeEmbedding)
SELECT lh.LoanId, AI_GENERATE_EMBEDDINGS(lh.LoanNarrative USE MODEL FoundryEmbeddingModel)
FROM dbo.LoanHistory lh
WHERE lh.LoanNarrative IS NOT NULL AND lh.LoanId BETWEEN 1 AND 5;
PRINT '  Rows 1-5 done '
GO

INSERT INTO dbo.LoanNarrativeEmbeddings (LoanId, NarrativeEmbedding)
SELECT lh.LoanId, AI_GENERATE_EMBEDDINGS(lh.LoanNarrative USE MODEL FoundryEmbeddingModel)
FROM dbo.LoanHistory lh
WHERE lh.LoanNarrative IS NOT NULL AND lh.LoanId BETWEEN 6 AND 10;
PRINT '  Rows 6-10 done '
GO

INSERT INTO dbo.LoanNarrativeEmbeddings (LoanId, NarrativeEmbedding)
SELECT lh.LoanId, AI_GENERATE_EMBEDDINGS(lh.LoanNarrative USE MODEL FoundryEmbeddingModel)
FROM dbo.LoanHistory lh
WHERE lh.LoanNarrative IS NOT NULL AND lh.LoanId BETWEEN 11 AND 15;
PRINT '  Rows 11-15 done '
GO

INSERT INTO dbo.LoanNarrativeEmbeddings (LoanId, NarrativeEmbedding)
SELECT lh.LoanId, AI_GENERATE_EMBEDDINGS(lh.LoanNarrative USE MODEL FoundryEmbeddingModel)
FROM dbo.LoanHistory lh
WHERE lh.LoanNarrative IS NOT NULL AND lh.LoanId BETWEEN 16 AND 20;
PRINT '  Rows 16-20 done '
GO

-- Rows 21-40 (HomeImprovement + additional Auto loans)
INSERT INTO dbo.LoanNarrativeEmbeddings (LoanId, NarrativeEmbedding)
SELECT lh.LoanId, AI_GENERATE_EMBEDDINGS(lh.LoanNarrative USE MODEL FoundryEmbeddingModel)
FROM dbo.LoanHistory lh
WHERE lh.LoanNarrative IS NOT NULL AND lh.LoanId BETWEEN 21 AND 25;
PRINT '  Rows 21-25 done '
GO

INSERT INTO dbo.LoanNarrativeEmbeddings (LoanId, NarrativeEmbedding)
SELECT lh.LoanId, AI_GENERATE_EMBEDDINGS(lh.LoanNarrative USE MODEL FoundryEmbeddingModel)
FROM dbo.LoanHistory lh
WHERE lh.LoanNarrative IS NOT NULL AND lh.LoanId BETWEEN 26 AND 30;
PRINT '  Rows 26-30 done '
GO

INSERT INTO dbo.LoanNarrativeEmbeddings (LoanId, NarrativeEmbedding)
SELECT lh.LoanId, AI_GENERATE_EMBEDDINGS(lh.LoanNarrative USE MODEL FoundryEmbeddingModel)
FROM dbo.LoanHistory lh
WHERE lh.LoanNarrative IS NOT NULL AND lh.LoanId BETWEEN 31 AND 35;
PRINT '  Rows 31-35 done '
GO

INSERT INTO dbo.LoanNarrativeEmbeddings (LoanId, NarrativeEmbedding)
SELECT lh.LoanId, AI_GENERATE_EMBEDDINGS(lh.LoanNarrative USE MODEL FoundryEmbeddingModel)
FROM dbo.LoanHistory lh
WHERE lh.LoanNarrative IS NOT NULL AND lh.LoanId BETWEEN 36 AND 40;
PRINT '  Rows 36-40 done '
GO

-- Rows 41-60 (more Auto + Personal loans)
INSERT INTO dbo.LoanNarrativeEmbeddings (LoanId, NarrativeEmbedding)
SELECT lh.LoanId, AI_GENERATE_EMBEDDINGS(lh.LoanNarrative USE MODEL FoundryEmbeddingModel)
FROM dbo.LoanHistory lh
WHERE lh.LoanNarrative IS NOT NULL AND lh.LoanId BETWEEN 41 AND 45;
PRINT '  Rows 41-45 done '
GO

INSERT INTO dbo.LoanNarrativeEmbeddings (LoanId, NarrativeEmbedding)
SELECT lh.LoanId, AI_GENERATE_EMBEDDINGS(lh.LoanNarrative USE MODEL FoundryEmbeddingModel)
FROM dbo.LoanHistory lh
WHERE lh.LoanNarrative IS NOT NULL AND lh.LoanId BETWEEN 46 AND 50;
PRINT '  Rows 46-50 done '
GO

INSERT INTO dbo.LoanNarrativeEmbeddings (LoanId, NarrativeEmbedding)
SELECT lh.LoanId, AI_GENERATE_EMBEDDINGS(lh.LoanNarrative USE MODEL FoundryEmbeddingModel)
FROM dbo.LoanHistory lh
WHERE lh.LoanNarrative IS NOT NULL AND lh.LoanId BETWEEN 51 AND 55;
PRINT '  Rows 51-55 done '
GO

INSERT INTO dbo.LoanNarrativeEmbeddings (LoanId, NarrativeEmbedding)
SELECT lh.LoanId, AI_GENERATE_EMBEDDINGS(lh.LoanNarrative USE MODEL FoundryEmbeddingModel)
FROM dbo.LoanHistory lh
WHERE lh.LoanNarrative IS NOT NULL AND lh.LoanId BETWEEN 56 AND 60;
PRINT '  Rows 56-60 done '
GO

-- Rows 61-80 (SmallBusiness loans)
INSERT INTO dbo.LoanNarrativeEmbeddings (LoanId, NarrativeEmbedding)
SELECT lh.LoanId, AI_GENERATE_EMBEDDINGS(lh.LoanNarrative USE MODEL FoundryEmbeddingModel)
FROM dbo.LoanHistory lh
WHERE lh.LoanNarrative IS NOT NULL AND lh.LoanId BETWEEN 61 AND 65;
PRINT '  Rows 61-65 done '
GO

INSERT INTO dbo.LoanNarrativeEmbeddings (LoanId, NarrativeEmbedding)
SELECT lh.LoanId, AI_GENERATE_EMBEDDINGS(lh.LoanNarrative USE MODEL FoundryEmbeddingModel)
FROM dbo.LoanHistory lh
WHERE lh.LoanNarrative IS NOT NULL AND lh.LoanId BETWEEN 66 AND 70;
PRINT '  Rows 66-70 done '
GO

INSERT INTO dbo.LoanNarrativeEmbeddings (LoanId, NarrativeEmbedding)
SELECT lh.LoanId, AI_GENERATE_EMBEDDINGS(lh.LoanNarrative USE MODEL FoundryEmbeddingModel)
FROM dbo.LoanHistory lh
WHERE lh.LoanNarrative IS NOT NULL AND lh.LoanId BETWEEN 71 AND 75;
PRINT '  Rows 71-75 done '
GO

INSERT INTO dbo.LoanNarrativeEmbeddings (LoanId, NarrativeEmbedding)
SELECT lh.LoanId, AI_GENERATE_EMBEDDINGS(lh.LoanNarrative USE MODEL FoundryEmbeddingModel)
FROM dbo.LoanHistory lh
WHERE lh.LoanNarrative IS NOT NULL AND lh.LoanId BETWEEN 76 AND 80;
PRINT '  Rows 76-80 done '
GO

-- Rows 81-100 (mixed Auto + Personal)
INSERT INTO dbo.LoanNarrativeEmbeddings (LoanId, NarrativeEmbedding)
SELECT lh.LoanId, AI_GENERATE_EMBEDDINGS(lh.LoanNarrative USE MODEL FoundryEmbeddingModel)
FROM dbo.LoanHistory lh
WHERE lh.LoanNarrative IS NOT NULL AND lh.LoanId BETWEEN 81 AND 85;
PRINT '  Rows 81-85 done '
GO

INSERT INTO dbo.LoanNarrativeEmbeddings (LoanId, NarrativeEmbedding)
SELECT lh.LoanId, AI_GENERATE_EMBEDDINGS(lh.LoanNarrative USE MODEL FoundryEmbeddingModel)
FROM dbo.LoanHistory lh
WHERE lh.LoanNarrative IS NOT NULL AND lh.LoanId BETWEEN 86 AND 90;
PRINT '  Rows 86-90 done '
GO

INSERT INTO dbo.LoanNarrativeEmbeddings (LoanId, NarrativeEmbedding)
SELECT lh.LoanId, AI_GENERATE_EMBEDDINGS(lh.LoanNarrative USE MODEL FoundryEmbeddingModel)
FROM dbo.LoanHistory lh
WHERE lh.LoanNarrative IS NOT NULL AND lh.LoanId BETWEEN 91 AND 95;
PRINT '  Rows 91-95 done '
GO

INSERT INTO dbo.LoanNarrativeEmbeddings (LoanId, NarrativeEmbedding)
SELECT lh.LoanId, AI_GENERATE_EMBEDDINGS(lh.LoanNarrative USE MODEL FoundryEmbeddingModel)
FROM dbo.LoanHistory lh
WHERE lh.LoanNarrative IS NOT NULL AND lh.LoanId BETWEEN 96 AND 100;
PRINT '  Rows 96-100 done.'
GO

-- Step 2c: Inspect the generated embeddings
SELECT
    e.LoanId,
    lh.LoanType,
    LEFT(lh.LoanNarrative, 60) + '...' AS NarrativePreview,
    DATALENGTH(e.NarrativeEmbedding) AS EmbeddingBytes,
    e.GeneratedAt
FROM dbo.LoanNarrativeEmbeddings e
JOIN dbo.LoanHistory lh ON e.LoanId = lh.LoanId
ORDER BY e.LoanId;
GO

-- Script 03 adds the vector index used for approximate similarity search.
PRINT '=== Embeddings generated. Next: build the DiskANN vector index (Step 3). ==='
GO
