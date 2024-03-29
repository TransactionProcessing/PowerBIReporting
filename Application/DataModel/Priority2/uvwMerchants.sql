CREATE OR ALTER VIEW [datamodel].[uvwMerchants]
AS
SELECT
	m.EstateReportingId,
	m.MerchantReportingId,
	m.Name as MerchantName,
	m.CreatedDateTime,
	ma.AddressLine1,
	ma.Town,
	ma.PostalCode,
	md.DeviceIdentifier,
	mc.Name as ContactName,
	CAST(t.TransactionDateTime as DATE) as LastSaleDate,
	CAST(t.TransactionDateTime as TIME) as LastSaleTime
from merchant m
inner join merchantaddress ma on ma.MerchantReportingId = m.MerchantReportingId
inner join merchantdevice md on md.MerchantReportingId = m.MerchantReportingId
inner join merchantcontact mc on mc.MerchantReportingId = m.MerchantReportingId
inner join (
			select MAX(TransactionDateTime) as TransactionDateTime, merchantreportingid
			from [transaction] t
			where t.TransactionDate > CAST(DATEADD(dd,-2, GETDATE()) as DATE)
			group by merchantreportingid) t on t.MerchantReportingId = m.MerchantReportingId
			

			