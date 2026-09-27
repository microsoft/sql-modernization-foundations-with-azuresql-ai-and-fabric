# Exercise 3.5 - Module Summary

You modernized ZavaFin incrementally without moving its operational data into a separate search platform and without changing the Zava Lending application.

## Prerequisites

- Complete [Exercise 3.4 - AI-Assisted Loan Scoring](<../04 - AI-Assisted Loan Scoring/README.md>).
- Keep the application running for the final review.

## What changed

| Before | After |
| --- | --- |
| Search depends on exact or closely related words | Embeddings capture semantic meaning |
| Full-text search and fixed ranking | Native vector search with a DiskANN index |
| Search and business filters are separate concerns | Hybrid search combines similarity and relational predicates |
| New vector data suggests index maintenance complexity | Normal DML adds rows and SQL maintains the vector index |
| Loan officers inspect applications manually | AI returns a grounded, advisory risk assessment |
| AI output could be hard to integrate | A stored procedure returns a stable relational contract |
| AI might be allowed to make changes | The workshop implementation is validated as side-effect-free |

## Modernizing the app without changing the code

The most important modernization outcome is not only the new AI capability. It is how the capability was introduced.

Throughout Module 3:

- the same application process remained running;
- the same **Applications** and **Historical Loans** screens were used;
- the same controllers and data repositories were used;
- `dbo.usp_LoanSearch` retained its application-facing contract;
- `dbo.usp_ScoreLoanApplication` exposed a conventional relational result; and
- database capability detection changed the badges and enabled the prepared UI.

The user experience improved because SQL became more capable. The application did not need a new build, new search service, new API tier, or redeployment between exercises.

## How Microsoft and SQL help

- **Security:** Azure SQL applies familiar database permissions and database-scoped credentials to model access.
- **Familiarity:** developers and database professionals use T-SQL to generate embeddings, search vectors, filter relational data, and call REST endpoints.
- **Reliability:** AI features build on the same mission-critical relational engine and stored procedure contracts already used by the application.
- **Integration:** external models and `sp_invoke_external_rest_endpoint` connect SQL to managed Microsoft Foundry endpoints.
- **Scalability:** DiskANN supports approximate nearest-neighbor search without scanning every vector.
- **Governance:** grounding, structured outputs, explicit validation, evidence visibility, and human review are designed into the workflow.

When these pieces come together, the database becomes an intelligent engine for the application rather than only a place to store data.

## Final application review

![Three application checkpoints showing Human Review, Hybrid Semantic Search, and AI-Assisted Scoring](<./images/app-modernization-journey.png>)

*Modernizing the app without changing the code: Azure SQL progressively adds compact Human Review, meaning-aware retrieval, and evidence-grounded AI-assisted scoring.*

Before moving on, confirm that:

- **Historical Loans** displays **Hybrid Semantic Search**;
- a paraphrased prompt returns semantically relevant results;
- structured filters still constrain the result set;
- **Applications** displays **AI-Assisted** after script `08`;
- an assessment includes both a recommendation and historical evidence;
- the loan officer remains responsible for the final decision; and
- evaluating an application does not change its status.

Stop the local application with `Ctrl+C`.

## Module summary

You:

- established a secure AI model connection from SQL;
- represented narrative meaning with native vectors;
- indexed and searched those vectors at scale;
- combined semantic and relational retrieval;
- grounded AI scoring in ZavaFin's own historical evidence;
- validated a responsible, advisory scoring boundary; and
- modernized the complete user experience without changing the code.

Congratulations! Continue to [Module 4 - Grow in Azure](<../../Module 4 - Grow in Azure/README.md>).
