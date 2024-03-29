CREATE OR ALTER VIEW [datamodel].[uvwTransactions]
AS
SELECT 
	t.TransactionId,
	t.TransactionReportingId,
	t.TransactionDateTime,
	t.TransactionDate,
	t.TransactionTime,
	DATEPART(hour, t.TransactionDateTime) as HourValue,
	t.MerchantReportingId,
	t.IsAuthorised,
	t.IsCompleted,
	t.ResponseCode,
	t.TransactionType,
	CAST(ISNULL(tar.Amount,0) as decimal) as G,
	CASE 
		WHEN t.OperatorIdentifier = 'PataPawa PostPay' AND CAST(ISNULL(tar.Amount,0) as decimal) > 250 THEN CAST(ISNULL(tar.Amount,0) as decimal) / 100
		ELSE CAST(ISNULL(tar.Amount,0) as decimal) 
	END as Amount,
	CASE t.OperatorIdentifier
		WHEN 'Voucher' THEN REPLACE(c.Description, ' Contract', '')
		ELSE COALESCE(t.OperatorIdentifier, '')
	END as OperatorIdentifier,
	t.ContractReportingId,
	c.OperatorId,
	s.SettlementDate,
	f.FeeValue,
	f.IsSettled,
	f.CalculatedValue,
	f.TransactionFeeReportingId
from [transaction] t
inner join contract c on c.ContractReportingId = t.ContractReportingId
left outer join transactionadditionalrequestdata tar on tar.TransactionReportingId = t.TransactionReportingId
left outer join merchantsettlementfee f on t.TransactionReportingId = f.TransactionReportingId
left outer join settlement s on s.SettlementReportingId = f.SettlementReportingId
