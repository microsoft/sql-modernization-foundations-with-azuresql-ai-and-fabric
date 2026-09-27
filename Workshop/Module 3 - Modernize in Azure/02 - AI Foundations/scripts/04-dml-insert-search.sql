/*
    Module 3 | Script 04 of 09 | Maintain embeddings during DML
    Purpose: Insert three historical loans and generate their narrative embeddings.
    Run once after script 03, against your assigned ZavaLendingDB-No database.
*/

-- Step 4a: Track the rows inserted by this script
IF OBJECT_ID('tempdb..#NewLoanIds') IS NOT NULL DROP TABLE #NewLoanIds;
CREATE TABLE #NewLoanIds (LoanId BIGINT);
GO

-- Step 4b: Insert representative historical loans
-- Distressed restaurant
INSERT INTO dbo.LoanHistory
    (ApplicantId, LoanType, RequestedAmount, ApprovedAmount, InterestRate,
     TermMonths, ApplicantIncome, CreditScore, DebtToIncomeRatio, EmploymentYears,
     LoanPurpose, LoanOutcome, DefaultRate, ApplicationDate, LoanNarrative)
OUTPUT inserted.LoanId INTO #NewLoanIds
VALUES
    (1, N'SmallBusiness', 85000.00, 85000.00, 9.50,
     48, 95000.00, 660, 0.42, 6.0,
     N'Working capital - restaurant recovery', N'Default', 0.1650,
     '2025-02-01',
     N'Family-owned restaurant seeking working capital after prolonged recovery period. Revenue declined 45% during the pandemic and has only recovered to 70% of pre-2020 levels. The owner has invested personal savings to keep the business afloat, resulting in depleted reserves and elevated personal debt. Two employees were laid off and the remaining staff is stretched thin. The landlord has been flexible on rent but that arrangement expires in 6 months. Despite loyal local customers, foot traffic in the area has not fully returned and competing delivery-only concepts have captured market share.');
GO

-- Excellent solar business
INSERT INTO dbo.LoanHistory
    (ApplicantId, LoanType, RequestedAmount, ApprovedAmount, InterestRate,
     TermMonths, ApplicantIncome, CreditScore, DebtToIncomeRatio, EmploymentYears,
     LoanPurpose, LoanOutcome, DefaultRate, ApplicationDate, LoanNarrative)
OUTPUT inserted.LoanId INTO #NewLoanIds
VALUES
    (2, N'SmallBusiness', 250000.00, 250000.00, 5.75,
     60, 180000.00, 790, 0.12, 15.0,
     N'Equipment purchase - solar installation', N'Active', 0.0150,
     '2025-03-10',
     N'Established solar installation company with 15 years of profitable operations and an impeccable credit profile. The business has grown 25% year-over-year for the past 3 years driven by federal tax incentives and increasing commercial demand. The owner has zero personal debt, owns their home outright, and maintains 18 months of operating reserves. The loan is for specialized equipment that will enable the company to take on utility-scale projects, expanding revenue potential by an estimated 40%. Three signed contracts are already in the pipeline for the new equipment.');
GO

-- Denied first-time buyer
INSERT INTO dbo.LoanHistory
    (ApplicantId, LoanType, RequestedAmount, ApprovedAmount, InterestRate,
     TermMonths, ApplicantIncome, CreditScore, DebtToIncomeRatio, EmploymentYears,
     LoanPurpose, LoanOutcome, DefaultRate, ApplicationDate, LoanNarrative)
OUTPUT inserted.LoanId INTO #NewLoanIds
VALUES
    (3, N'Personal', 25000.00, NULL, NULL,
     36, 52000.00, 620, 0.49, 1.5,
     N'Down payment assistance', N'Denied', NULL,
     '2025-03-15',
     N'Young borrower seeking a personal loan to fund a down payment on a first home purchase. Credit score is below average due to student loan delinquencies and a short credit history of only 18 months. Current rent consumes 40% of gross income and adding a mortgage plus this loan payment would push total debt-to-income above 55%. The applicant has no savings beyond a small emergency fund. Employment is stable but the position is entry-level with limited upward mobility in the near term. No co-signer available and no collateral to secure the loan.');
GO

-- Step 4c: Generate embeddings; SQL maintains the vector index automatically.
INSERT INTO dbo.LoanNarrativeEmbeddings (LoanId, NarrativeEmbedding)
SELECT lh.LoanId,
       AI_GENERATE_EMBEDDINGS(lh.LoanNarrative USE MODEL FoundryEmbeddingModel)
FROM dbo.LoanHistory lh
WHERE lh.LoanId IN (SELECT LoanId FROM #NewLoanIds);
GO

-- Step 4d: Inspect the inserted vectors
SELECT
    e.LoanId,
    lh.LoanType,
    LEFT(lh.LoanNarrative, 60) + '...' AS NarrativePreview,
    DATALENGTH(e.NarrativeEmbedding) AS EmbeddingBytes,
    e.GeneratedAt
FROM dbo.LoanNarrativeEmbeddings e
JOIN dbo.LoanHistory lh ON e.LoanId = lh.LoanId
WHERE lh.LoanId IN (SELECT LoanId FROM #NewLoanIds)
ORDER BY e.LoanId;
GO

PRINT '=== Additional 3 rows inserted and embeddings generated. ==='
GO
