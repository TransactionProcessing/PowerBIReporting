CREATE OR ALTER VIEW [datamodel].[uvwFile]
AS
SELECT 
	f.FileReportingId,
	f.FileReceivedDateTime, 
	[FileReceivedDate] = CONVERT(DATE,f.FileReceivedDate),
	[FileReceivedTime] = CONVERT(TIME, f.FileReceivedDateTime), 
	f.IsCompleted,
	OriginalFileName as FileName,
	estatesecurityuser.EmailAddress as EmailAddress, 
	count(fileline.LineNumber) as LineCount, 
	SUM(case fileline.Status WHEN 'P' THEN 1 ELSE 0 END) as PendingCount,
	SUM(case fileline.Status WHEN 'F' THEN 1 ELSE 0 END) as FailedCount,
	SUM(case fileline.Status WHEN 'S' THEN 1 ELSE 0 END) as SuccessCount,
	f.MerchantReportingId,
	f.FileImportLogReportingId
from fileimportlogfile
left outer join estatesecurityuser on estatesecurityuser.SecurityUserId = fileimportlogfile.UserId
inner join [file] f on f.FileReportingId = fileimportlogfile.FileReportingId
inner join fileline on fileline.FileReportingId = f.FileReportingId
group by f.MerchantReportingId, OriginalFileName, estatesecurityuser.EmailAddress, f.FileReportingId, f.FileReceivedDateTime, f.FileReceivedDate, f.IsCompleted, f.FileImportLogReportingId

