/*
    Module 3 | Script 01 of 09 | Register the external embedding model
    Purpose: Register the Foundry embedding endpoint and verify model access.
    Run first, against your assigned ZavaLendingDB-No database.
*/

-- Step 1a: Register the external model
PRINT '=== Creating External Model for text-embedding-3-large ==='
GO

-- Replace the model on reruns so its endpoint and settings stay current.
IF EXISTS (SELECT 1 FROM sys.external_models WHERE name = 'FoundryEmbeddingModel')
    DROP EXTERNAL MODEL [FoundryEmbeddingModel];

IF NOT EXISTS (SELECT 1 FROM sys.database_scoped_credentials WHERE name = 'https://foundry-zava-38e39j.openai.azure.com/')
    THROW 50001, 'Required database-scoped credential does not exist.', 1;
GO

CREATE EXTERNAL MODEL [FoundryEmbeddingModel]
WITH (
    LOCATION = 'https://foundry-zava-38e39j.openai.azure.com/openai/deployments/text-embedding-3-large/embeddings?api-version=2024-02-01',
    API_FORMAT = 'Azure OpenAI',
    MODEL_TYPE = EMBEDDINGS,
    MODEL = 'text-embedding-3-large',
    CREDENTIAL = [https://foundry-zava-38e39j.openai.azure.com/]
);
PRINT '  External model registered.'
GO

-- Step 1b: Verify model access
DECLARE @prompt NVARCHAR(1000) = 'This is a test';
SELECT AI_GENERATE_EMBEDDINGS( @Prompt USE MODEL FoundryEmbeddingModel );