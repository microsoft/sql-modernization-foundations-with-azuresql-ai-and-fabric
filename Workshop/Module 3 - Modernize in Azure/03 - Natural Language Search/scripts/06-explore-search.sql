/*
    Module 3 | Script 06 of 09 | Explore hybrid search
    Purpose: Compare pure semantic retrieval with semantic plus relational filters.
    Run after script 05, against your assigned ZavaLendingDB-No database.
*/

-- Step 6a: Search by meaning without relational filters
PRINT '=== TEST 1: Pure semantic search ==='
GO

EXEC dbo.usp_LoanSearch
    @Prompt = N'borrower was financially stretched and had trouble making payments',
    @TopN = 5;
GO

-- Step 6b: Combine meaning with business filters
PRINT '=== TEST 2: Semantic search with business filters ==='
GO

EXEC dbo.usp_LoanSearch
    @Prompt = N'growing business expanding operations with strong revenue trajectory',
    @LoanType = 'SmallBusiness',
    @MinAmount = 100000,
    @DateFrom = '2024-01-01',
    @TopN = 5;
GO

-- Confirm that the ranking reflects meaning and every row satisfies the filters.
