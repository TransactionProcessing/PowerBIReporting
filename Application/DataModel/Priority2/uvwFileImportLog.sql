CREATE OR ALTER VIEW [datamodel].[uvwFileImportLog]
AS
SELECT 
	fileimportlog.FileImportLogReportingId, 
	fileimportlog.ImportLogDateTime,
	[ImportLogDate] = CONVERT(DATE,fileimportlog.ImportLogDateTime),
	[ImportLogTime] = CONVERT(TIME, fileimportlog.ImportLogDateTime), 
	COUNT(fileimportlogfile.FileReportingId) as FileCount,
	f.MerchantReportingId	
from fileimportlog
inner join fileimportlogfile on fileimportlogfile.FileImportLogReportingId = fileimportlog.FileImportLogReportingId
inner join [file] f on f.FileReportingId = fileimportlogfile.FileReportingId
group by fileimportlog.FileImportLogReportingId, fileimportlog.ImportLogDateTime, f.MerchantReportingId



