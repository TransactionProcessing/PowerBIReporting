CREATE OR ALTER VIEW [dbo].[uvwOperators]
AS
SELECT 
	estateoperator.OperatorId as OperatorId,
	estateoperator.Name as OperatorName
from estateoperator
GO
