CREATE OR ALTER VIEW [datamodel].[uvwSettlements]
AS

SELECT
	s.IsCompleted as SettlementCompleted,
	s.SettlementId,
	s.SettlementReportingId,
	s.SettlementDate,
	f.CalculatedValue,
	f.IsSettled,
	f.MerchantReportingId,
	f.TransactionFeeReportingId,
	f.TransactionReportingId,
	CASE t.OperatorIdentifier
		WHEN 'Voucher' THEN REPLACE(c.Description, ' Contract', '')
		ELSE COALESCE(t.OperatorIdentifier, '')
	END as OperatorIdentifier,
	c.OperatorId
from settlement s 
inner join merchantsettlementfee f on s.SettlementReportingId = f.SettlementReportingId
inner join [transaction] t on t.TransactionReportingId = f.TransactionReportingId
inner join contract c on c.ContractReportingId = t.ContractReportingId


