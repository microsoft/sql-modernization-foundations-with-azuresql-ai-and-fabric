/*
    Module 3 | Script 05 of 09 | Upgrade search to hybrid retrieval
    Purpose: Replace full-text search with vector similarity plus relational filters.
    Run after script 04, against your assigned ZavaLendingDB-No database.

    This script has two versions of the same stored procedure: the legacy full-text search version and the new hybrid search version.

    You can choose which version to use by executing the corresponding CREATE OR ALTER PROCEDURE statement.
*/

-- Step 5a: Establish the full-text search baseline
PRINT '=== Creating Legacy version of usp_LoanSearch ==='
GO

CREATE OR ALTER PROCEDURE dbo.usp_LoanSearch
    @Prompt             NVARCHAR(1000),
    @LoanType           NVARCHAR(30)  = NULL,
    @MinCreditScore     INT           = NULL,
    @MaxCreditScore     INT           = NULL,
    @LoanOutcome        NVARCHAR(20)  = NULL,
    @MinAmount          DECIMAL(18,2) = NULL,
    @MaxAmount          DECIMAL(18,2) = NULL,
    @DateFrom           DATE          = NULL,
    @DateTo             DATE          = NULL,
    @TopN               INT           = 10
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@TopN)
        lh.LoanId,
        lh.LoanType,
        lh.RequestedAmount,
        lh.ApprovedAmount,
        lh.CreditScore,
        lh.DebtToIncomeRatio,
        lh.LoanOutcome,
        lh.DefaultRate,
        lh.LoanPurpose,
        lh.ApplicationDate,
        LEFT(lh.LoanNarrative, 200) AS NarrativePreview,
        lh.LoanNarrative AS LoanNarrative,
        CAST(NULL AS FLOAT) AS SemanticDistance,
        CAST('Full-Text Match' AS NVARCHAR(50)) AS SemanticRelevance
    FROM dbo.LoanHistory lh
    INNER JOIN FREETEXTTABLE
    (
        dbo.LoanHistory,
        LoanNarrative,
        @Prompt
    ) ft
        ON ft.[KEY] = lh.LoanId
    WHERE 1 = 1
      AND (@LoanType       IS NULL OR lh.LoanType      = @LoanType)
      AND (@MinCreditScore IS NULL OR lh.CreditScore   >= @MinCreditScore)
      AND (@MaxCreditScore IS NULL OR lh.CreditScore   <= @MaxCreditScore)
      AND (@LoanOutcome    IS NULL OR lh.LoanOutcome   = @LoanOutcome)
      AND (@MinAmount      IS NULL OR lh.RequestedAmount >= @MinAmount)
      AND (@MaxAmount      IS NULL OR lh.RequestedAmount <= @MaxAmount)
      AND (@DateFrom       IS NULL OR lh.ApplicationDate >= @DateFrom)
      AND (@DateTo         IS NULL OR lh.ApplicationDate <= @DateTo)
    ORDER BY lh.ApplicationDate DESC;

END
GO

PRINT '  Legacy procedure created.'
GO

-- Exact narrative words produce full-text matches.
EXEC dbo.usp_LoanSearch
    @Prompt = N'borrower was financially stretched',
    @LoanType = 'SmallBusiness',
    @MinCreditScore = 600,
    @TopN = 5;
GO

-- Different vocabulary for the same meaning exposes the full-text limitation.
EXEC dbo.usp_LoanSearch
    @Prompt = N'utterly tapped out and drowning in red ink',
    @LoanType = 'SmallBusiness',
    @MinCreditScore = 600,
    @TopN = 5;
GO

-- Step 5b: Preserve the contract and replace its implementation with hybrid search
PRINT '=== Creating usp_LoanSearch as Hybrid ==='
GO

CREATE OR ALTER PROCEDURE dbo.usp_LoanSearch
    @Prompt             NVARCHAR(1000),       -- Natural language search prompt
    @LoanType           NVARCHAR(30)  = NULL, -- Optional: Auto, Personal, SmallBusiness
    @MinCreditScore     INT           = NULL, -- Optional: minimum credit score filter
    @MaxCreditScore     INT           = NULL, -- Optional: maximum credit score filter
    @LoanOutcome        NVARCHAR(20)  = NULL, -- Optional: Approved, Denied, Default, PaidInFull, Active
    @MinAmount          DECIMAL(18,2) = NULL, -- Optional: minimum requested amount
    @MaxAmount          DECIMAL(18,2) = NULL, -- Optional: maximum requested amount
    @DateFrom           DATE          = NULL, -- Optional: application date range start
    @DateTo             DATE          = NULL, -- Optional: application date range end
    @TopN               INT           = 10    -- Number of results (default 10)
AS
BEGIN
    SET NOCOUNT ON;

    -- Embed the prompt with the same model used for the stored narratives.
    DECLARE @promptEmbedding VECTOR(3072, float16);

    SET @promptEmbedding = AI_GENERATE_EMBEDDINGS(
        @Prompt USE MODEL FoundryEmbeddingModel
    );

    -- Apply relational predicates during approximate graph traversal.
    SELECT TOP(@TopN) WITH APPROXIMATE
        lh.LoanId,
        lh.LoanType,
        lh.RequestedAmount,
        lh.ApprovedAmount,
        lh.CreditScore,
        lh.DebtToIncomeRatio,
        lh.LoanOutcome,
        lh.DefaultRate,
        lh.LoanPurpose,
        lh.ApplicationDate,
        LEFT(lh.LoanNarrative, 200) AS NarrativePreview,
        lh.LoanNarrative AS LoanNarrative,
        vs.distance AS SemanticDistance,
        CASE
            WHEN vs.distance < 0.15 THEN 'Very High'
            WHEN vs.distance < 0.30 THEN 'High'
            WHEN vs.distance < 0.45 THEN 'Moderate'
            ELSE 'Low'
        END AS SemanticRelevance
    FROM VECTOR_SEARCH(
        TABLE = dbo.LoanNarrativeEmbeddings AS e,
        COLUMN = NarrativeEmbedding,
        SIMILAR_TO = @promptEmbedding,
        METRIC = 'cosine'
    ) AS vs
    JOIN dbo.LoanHistory lh ON e.LoanId = lh.LoanId
    WHERE 1 = 1
      -- Keep only semantic matches that also satisfy the business filters.
      AND (@LoanType      IS NULL OR lh.LoanType      = @LoanType)
      AND (@MinCreditScore IS NULL OR lh.CreditScore   >= @MinCreditScore)
      AND (@MaxCreditScore IS NULL OR lh.CreditScore   <= @MaxCreditScore)
      AND (@LoanOutcome    IS NULL OR lh.LoanOutcome   = @LoanOutcome)
      AND (@MinAmount      IS NULL OR lh.RequestedAmount >= @MinAmount)
      AND (@MaxAmount      IS NULL OR lh.RequestedAmount <= @MaxAmount)
      AND (@DateFrom       IS NULL OR lh.ApplicationDate >= @DateFrom)
      AND (@DateTo         IS NULL OR lh.ApplicationDate <= @DateTo)
    ORDER BY vs.distance;
END
GO

PRINT '=== Hybrid search procedure created ==='
GO

-- Step 5c: Re-run the failed prompt against hybrid search
EXEC dbo.usp_LoanSearch
    @Prompt = N'utterly tapped out and drowning in red ink',
    @LoanType = 'SmallBusiness',
    @MinCreditScore = 600,
    @TopN = 5;
GO
