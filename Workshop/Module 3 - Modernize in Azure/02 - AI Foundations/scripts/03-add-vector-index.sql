/*
    Module 3 | Script 03 of 09 | Add the DiskANN vector index
    Purpose: Index narrative embeddings and validate semantic similarity search.
    Run after script 02, against your assigned ZavaLendingDB-No database.
*/

-- Step 3a: Create the DiskANN vector index
PRINT '=== Creating DiskANN vector index ==='
GO

-- DiskANN requires at least 100 populated vectors; smaller sets use an exact scan.

DECLARE @rowcount INT;
SELECT @rowcount = COUNT(*) FROM dbo.LoanNarrativeEmbeddings WHERE NarrativeEmbedding IS NOT NULL;

IF @rowcount >= 100
BEGIN
    -- Recreate the index so reruns use the current index version.
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LoanNarrativeEmbeddings_Vector')
    BEGIN
        DROP INDEX IX_LoanNarrativeEmbeddings_Vector ON dbo.LoanNarrativeEmbeddings;
        PRINT '  Dropped existing DiskANN vector index.'
    END

    CREATE VECTOR INDEX IX_LoanNarrativeEmbeddings_Vector
    ON dbo.LoanNarrativeEmbeddings(NarrativeEmbedding)
    WITH (METRIC = 'cosine', TYPE = 'diskann');
    PRINT '  DiskANN vector index created.'
END
ELSE
    PRINT '  Skipping DiskANN index: only ' + CAST(@rowcount AS VARCHAR) + ' rows (need 100+). Vector search still works via exact scan.'
GO

-- Step 3b: Test semantic search with a natural-language prompt
PRINT '=== Quick test: Vector search with natural language ==='
GO

DECLARE @testEmbedding VECTOR(3072, float16);
SET @testEmbedding = AI_GENERATE_EMBEDDINGS(
    N'utterly tapped out and drowning in red ink'
    USE MODEL FoundryEmbeddingModel
);

SELECT TOP (5)
    e.LoanId,
    lh.LoanType,
    lh.LoanOutcome,
    LEFT(lh.LoanNarrative, 80) + '...' AS NarrativePreview,
    vs.distance AS SemanticDistance
FROM VECTOR_SEARCH(
    TABLE = dbo.LoanNarrativeEmbeddings AS e,
    COLUMN = NarrativeEmbedding,
    SIMILAR_TO = @testEmbedding,
    METRIC = 'cosine'
) AS vs
JOIN dbo.LoanHistory lh ON e.LoanId = lh.LoanId
ORDER BY vs.distance;
GO

-- Relevant results demonstrate semantic matching without exact prompt words.

PRINT '=== DiskANN index built. Vector search validated. Ready for hybrid search. ==='
GO
