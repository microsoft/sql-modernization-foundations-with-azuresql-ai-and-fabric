/*
    Module 3 | Script 08 of 09 | Deploy AI-assisted loan scoring
    Purpose: Ground a Foundry assessment with similar loans and return one advisory row.
    Run after script 07, against your assigned ZavaLendingDB-No database.
*/

-- Step 8: Create the application's stable, side-effect-free scoring contract.
PRINT '=== Creating AI-assisted dbo.usp_ScoreLoanApplication ===';
GO

CREATE OR ALTER PROCEDURE dbo.usp_ScoreLoanApplication
    @ApplicationId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    -- Validate the request and load the current application facts.
    IF @ApplicationId IS NULL OR @ApplicationId <= 0
    BEGIN
        THROW 50001, 'A valid loan application ID is required.', 1;
    END;

    DECLARE @StartedAt DATETIME2(7) = SYSUTCDATETIME();
    DECLARE @LoanType NVARCHAR(30);
    DECLARE @RequestedAmount DECIMAL(18,2);
    DECLARE @ApplicantIncome DECIMAL(18,2);
    DECLARE @CreditScore INT;
    DECLARE @DebtToIncomeRatio DECIMAL(5,2);
    DECLARE @LoanPurpose NVARCHAR(200);
    DECLARE @ApplicantId INT;

    SELECT
        @LoanType = la.LoanType,
        @RequestedAmount = la.RequestedAmount,
        @LoanPurpose = la.LoanPurpose,
        @ApplicantIncome = a.AnnualIncome,
        @CreditScore = a.CreditScore,
        @DebtToIncomeRatio = a.DebtToIncomeRatio,
        @ApplicantId = la.ApplicantId
    FROM dbo.LoanApplications AS la
    INNER JOIN dbo.Applicants AS a
        ON a.ApplicantId = la.ApplicantId
    WHERE la.ApplicationId = @ApplicationId;

    IF @ApplicantId IS NULL
    BEGIN
        THROW 50001, 'The selected loan application does not exist.', 1;
    END;

    -- Describe the application and embed it for similarity search.
    DECLARE @SearchPrompt NVARCHAR(1000) =
        @LoanType + N' loan for $' + FORMAT(@RequestedAmount, 'N0') +
        N', income $' + FORMAT(@ApplicantIncome, 'N0') +
        N', credit score ' + CAST(@CreditScore AS NVARCHAR(10)) +
        N', DTI ' + COALESCE(CAST(@DebtToIncomeRatio AS NVARCHAR(10)), N'not provided') +
        N', purpose: ' + COALESCE(@LoanPurpose, N'general');

    DECLARE @PromptEmbedding VECTOR(3072, float16);
    SET @PromptEmbedding = AI_GENERATE_EMBEDDINGS(
        @SearchPrompt USE MODEL FoundryEmbeddingModel
    );

    -- Retrieve the ten most similar historical loans.
    DECLARE @SimilarLoans TABLE
    (
        LoanId BIGINT,
        LoanType NVARCHAR(30),
        LoanOutcome NVARCHAR(20),
        RequestedAmount DECIMAL(18,2),
        ApplicantIncome DECIMAL(18,2),
        CreditScore INT,
        DebtToIncomeRatio DECIMAL(5,2),
        DefaultRate DECIMAL(5,4),
        LoanPurpose NVARCHAR(200),
        NarrativePreview NVARCHAR(200),
        SemanticDistance FLOAT
    );

    INSERT INTO @SimilarLoans
    SELECT TOP (10) WITH APPROXIMATE
        lh.LoanId,
        lh.LoanType,
        lh.LoanOutcome,
        lh.RequestedAmount,
        lh.ApplicantIncome,
        lh.CreditScore,
        lh.DebtToIncomeRatio,
        lh.DefaultRate,
        lh.LoanPurpose,
        LEFT(lh.LoanNarrative, 200),
        vs.distance
    FROM VECTOR_SEARCH(
        TABLE = dbo.LoanNarrativeEmbeddings AS e,
        COLUMN = NarrativeEmbedding,
        SIMILAR_TO = @PromptEmbedding,
        METRIC = 'cosine'
    ) AS vs
    INNER JOIN dbo.LoanHistory AS lh
        ON lh.LoanId = e.LoanId
    ORDER BY vs.distance;

    -- Aggregate approval and default evidence in SQL.
    DECLARE @SimilarLoansAnalyzed INT;
    DECLARE @SimilarLoanApprovalRate DECIMAL(5,2);
    DECLARE @SimilarLoanAvgDefaultRate DECIMAL(5,4);

    SELECT
        @SimilarLoansAnalyzed = COUNT(*),
        @SimilarLoanApprovalRate = CONVERT(
            DECIMAL(5,2),
            SUM(CASE WHEN LoanOutcome IN (N'Approved', N'PaidInFull') THEN 1.0 ELSE 0.0 END)
                / NULLIF(COUNT(*), 0) * 100),
        @SimilarLoanAvgDefaultRate = CONVERT(DECIMAL(5,4), AVG(DefaultRate))
    FROM @SimilarLoans;

    -- Format the retrieved evidence for the grounded model prompt.
    DECLARE @SimilarLoansSummary NVARCHAR(MAX) = N'';

    SELECT
        @SimilarLoansSummary = @SimilarLoansSummary +
            N'Loan ' + CAST(LoanId AS NVARCHAR(20)) + N': ' +
            N'$' + FORMAT(RequestedAmount, 'N0') + N', ' +
            N'Income $' + FORMAT(ApplicantIncome, 'N0') + N', ' +
            N'Credit ' + CAST(CreditScore AS NVARCHAR(10)) + N', ' +
            N'DTI ' + COALESCE(CAST(DebtToIncomeRatio AS NVARCHAR(10)), N'not provided') + N', ' +
            N'Purpose: ' + COALESCE(LoanPurpose, N'N/A') + N', ' +
            N'Outcome: ' + LoanOutcome +
            CASE
                WHEN DefaultRate IS NOT NULL
                    THEN N', Default Risk: ' + CAST(DefaultRate AS NVARCHAR(10))
                ELSE N''
            END +
            NCHAR(10)
    FROM @SimilarLoans
    ORDER BY SemanticDistance;

    IF @SimilarLoansSummary = N''
    BEGIN
        SET @SimilarLoansSummary = N'No similar historical loans were available.';
    END;

    DECLARE @Prompt NVARCHAR(MAX) = N'You are a loan underwriting AI assistant for ZavaFin.
Analyze this loan application against similar historical loans and provide a risk assessment.

APPLICATION:
- Loan Type: ' + @LoanType + N'
- Requested Amount: $' + FORMAT(@RequestedAmount, 'N0') + N'
- Applicant Income: $' + FORMAT(@ApplicantIncome, 'N0') + N'
- Credit Score: ' + CAST(@CreditScore AS NVARCHAR(10)) + N'
- Debt-to-Income Ratio: ' + COALESCE(CAST(@DebtToIncomeRatio AS NVARCHAR(10)), N'Not provided') + N'
- Purpose: ' + COALESCE(@LoanPurpose, N'Not specified') + N'

SIMILAR HISTORICAL LOANS (' + CAST(@SimilarLoansAnalyzed AS NVARCHAR(10)) + N' matches):
' + @SimilarLoansSummary + N'

AGGREGATE METRICS:
- Approval rate among similar loans: ' +
    COALESCE(CAST(@SimilarLoanApprovalRate AS NVARCHAR(10)) + N'%', N'Unavailable') + N'
- Average default rate: ' +
    COALESCE(CAST(@SimilarLoanAvgDefaultRate AS NVARCHAR(10)), N'Unavailable') + N'

Provide a concise risk narrative (3-4 sentences) that:
1. References the applicant''s stated loan purpose and loan type
2. References patterns from the similar loans when evidence is available
3. Identifies key risk factors
4. States a clear recommendation
5. Is written for a human underwriter to review

Respond with ONLY a JSON object:
{"risk_score": <0-100>, "risk_category": "<Low|Medium|High|Critical>", "decision": "<Approved|ConditionallyApproved|ManualReview|Denied>", "narrative": "<your narrative>"}';

    -- Call the governed Foundry endpoint with the application and evidence.
    DECLARE @Payload NVARCHAR(MAX) = N'{
        "model": "gpt-5.4-mini",
        "messages": [
            {"role": "system", "content": "You are a precise loan risk assessment engine. Always respond with valid JSON only."},
            {"role": "user", "content": "' + STRING_ESCAPE(@Prompt, 'json') + N'"}
        ],
        "max_completion_tokens": 500
    }';
    DECLARE @Response NVARCHAR(MAX);
    DECLARE @RestReturnCode INT;

    EXEC @RestReturnCode = sp_invoke_external_rest_endpoint
        @url = N'https://foundry-zava-38e39j.openai.azure.com/openai/v1/chat/completions',
        @method = 'POST',
        @credential = [https://foundry-zava-38e39j.openai.azure.com/],
        @payload = @Payload,
        @timeout = 120,
        @response = @Response OUTPUT
    WITH RESULT SETS NONE;

    IF @RestReturnCode <> 0
    BEGIN
        THROW 50002, 'AI-assisted scoring could not be completed.', 1;
    END;

    -- Extract and validate the model's JSON response.
    DECLARE @Content NVARCHAR(MAX);
    DECLARE @Result NVARCHAR(MAX) = JSON_QUERY(@Response, '$.result');

    SELECT
        @Content = parsed.Content
    FROM OPENJSON(COALESCE(@Result, @Response), '$.choices')
    WITH
    (
        Content NVARCHAR(MAX) '$.message.content'
    ) AS parsed;

    SET @Content = LTRIM(RTRIM(
        REPLACE(
            REPLACE(COALESCE(@Content, N''), N'```json', N''),
            N'```',
            N'')));

    IF ISJSON(@Content) <> 1
    BEGIN
        THROW 50003, 'AI-assisted scoring returned an invalid assessment.', 1;
    END;

    DECLARE @RiskScore DECIMAL(5,2);
    DECLARE @RiskCategoryRaw NVARCHAR(50);
    DECLARE @DecisionRaw NVARCHAR(50);
    DECLARE @AssessmentNarrative NVARCHAR(MAX);

    SELECT
        @RiskScore = parsed.RiskScore,
        @RiskCategoryRaw = parsed.RiskCategory,
        @DecisionRaw = parsed.Decision,
        @AssessmentNarrative = parsed.AssessmentNarrative
    FROM OPENJSON(@Content)
    WITH
    (
        RiskScore DECIMAL(5,2) '$.risk_score',
        RiskCategory NVARCHAR(50) '$.risk_category',
        Decision NVARCHAR(50) '$.decision',
        AssessmentNarrative NVARCHAR(MAX) '$.narrative'
    ) AS parsed;

    -- Normalize model wording to the application's controlled vocabulary.
    DECLARE @DecisionKey NVARCHAR(50) = LOWER(
        REPLACE(
            REPLACE(
                REPLACE(LTRIM(RTRIM(COALESCE(@DecisionRaw, N''))), N' ', N''),
                N'-',
                N''),
            N'_',
            N''));
    DECLARE @RiskCategoryKey NVARCHAR(50) = LOWER(
        REPLACE(
            REPLACE(LTRIM(RTRIM(COALESCE(@RiskCategoryRaw, N''))), N' ', N''),
            N'-',
            N''));
    DECLARE @Decision NVARCHAR(30) =
        CASE @DecisionKey
            WHEN N'approve' THEN N'Approved'
            WHEN N'approved' THEN N'Approved'
            WHEN N'conditionallyapprove' THEN N'ConditionallyApproved'
            WHEN N'conditionallyapproved' THEN N'ConditionallyApproved'
            WHEN N'conditionalapproval' THEN N'ConditionallyApproved'
            WHEN N'manualreview' THEN N'ManualReview'
            WHEN N'review' THEN N'ManualReview'
            WHEN N'deny' THEN N'Denied'
            WHEN N'denied' THEN N'Denied'
            WHEN N'reject' THEN N'Denied'
            WHEN N'rejected' THEN N'Denied'
            ELSE NULL
        END;
    DECLARE @RiskCategory NVARCHAR(20) =
        CASE @RiskCategoryKey
            WHEN N'low' THEN N'Low'
            WHEN N'medium' THEN N'Medium'
            WHEN N'high' THEN N'High'
            WHEN N'critical' THEN N'Critical'
            ELSE NULL
        END;

    IF @RiskScore IS NULL
       OR @RiskScore < 0
       OR @RiskScore > 100
       OR @Decision IS NULL
       OR @RiskCategory IS NULL
       OR NULLIF(LTRIM(RTRIM(@AssessmentNarrative)), N'') IS NULL
    BEGIN
        THROW 50003, 'AI-assisted scoring returned an invalid assessment.', 1;
    END;

    -- Calculate amount and rate recommendations deterministically in SQL.
    DECLARE @RecommendedAmount DECIMAL(18,2) =
        CASE @Decision
            WHEN N'Approved' THEN @RequestedAmount
            WHEN N'ConditionallyApproved' THEN CONVERT(DECIMAL(18,2), @RequestedAmount * 0.80)
            ELSE NULL
        END;
    DECLARE @RecommendedRate DECIMAL(5,2) =
        CASE @Decision
            WHEN N'Approved' THEN CONVERT(DECIMAL(5,2), 5.50 + (@RiskScore / 25.00))
            WHEN N'ConditionallyApproved' THEN CONVERT(DECIMAL(5,2), 6.75 + (@RiskScore / 20.00))
            ELSE NULL
        END;

    -- Return one stable, advisory result row without persisting a decision.
    SELECT
        ISNULL(CONVERT(BIGINT, @ApplicationId), CONVERT(BIGINT, 0)) AS ApplicationId,
        ISNULL(CONVERT(NVARCHAR(30), @Decision), N'') AS Decision,
        ISNULL(CONVERT(DECIMAL(5,2), @RiskScore), CONVERT(DECIMAL(5,2), 0)) AS RiskScore,
        ISNULL(CONVERT(NVARCHAR(20), @RiskCategory), N'') AS RiskCategory,
        CONVERT(DECIMAL(18,2), @RecommendedAmount) AS RecommendedAmount,
        CONVERT(DECIMAL(5,2), @RecommendedRate) AS RecommendedRate,
        ISNULL(CONVERT(NVARCHAR(MAX), @AssessmentNarrative), N'') AS AssessmentNarrative,
        CONVERT(INT, NULLIF(@SimilarLoansAnalyzed, 0)) AS SimilarLoansAnalyzed,
        CONVERT(DECIMAL(5,2), @SimilarLoanApprovalRate) AS SimilarLoanApprovalRate,
        CONVERT(DECIMAL(5,4), @SimilarLoanAvgDefaultRate) AS SimilarLoanAvgDefaultRate,
        ISNULL(
            CONVERT(INT, DATEDIFF(MILLISECOND, @StartedAt, SYSUTCDATETIME())),
            0) AS ProcessingTimeMs;
END;
GO

PRINT '=== AI-assisted dbo.usp_ScoreLoanApplication created ===';
GO
