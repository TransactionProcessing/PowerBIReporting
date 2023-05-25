IF OBJECT_ID('dbo.uvwDepartmentSalesReport', 'V') IS NOT NULL 
  DROP VIEW dbo.uvwDepartmentSalesReport; 
GO; 

CREATE VIEW [dbo].[uvwDepartmentSalesReport]
AS
SELECT ActivityDate,
	   DepartmentId,
	   SUM(LineTotalExc) as SalesNet, 
	   SUM(salesCount) as CustomerCount,  
	   SUM(LineTotalInc) as SalesGross,
	   SUM(LineTotalExc) / CASE SUM(salesCount) WHEN 0 THEN 1 ELSE SUM(salesCount) END as AvgSpend
from [dbo].[ProductSummaryDaily]
group by ActivityDate,DepartmentId