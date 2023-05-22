<#
.SYNOPSIS
  Powershell Script that will loop through a folder and run SQL scripts against a database
.Description
  This script was developed to be used with VSTS. It must accept parameters, since VSTS variables cannot be used within scripts. Developers can specify a different SQL folder with each build
.NOTES
  Version:        1.0.1
  Author:         jcefoli
  Creation Date:  3/16/2017
.PARAMETER ConnectionString
  ConnectionString of database to run scripts against
.PARAMETER SQLFilePath
  First Part of the SQL File Path. For example, "E:\Sql"
.PARAMETER SQLFolderSuffix
  A subfolder under the SQL script root
.PARAMETER SQLExcludePattern
  Pattern to exclude for filename match. Pass a blank string to include everything
.EXAMPLE
  ./Run_SQL.ps1 -ConnectionString "Data Source=localhost,1433; Initial Catalog=my_db; Integrated Security=True" -SQLFilePath "E:\SQL" -SQLFolderSuffix "v1_1" -SQLExcludePattern "*RunOnce*"
  This will recursively run all scripts except those with filenames matching "*RunOnce*" in E:\SQL\v1_1 against the database specified in the connectionString. -SQLFolderSuffix may be a NULL string
#>

param (
    [string]
    $ConnectionString = $(throw "-ConnectionString is required."),
    [string]
    $SQLFilePath = $(throw "-SQLFilePath is required."),
    [string]
    $SQLFolderSuffix = $(throw "-SQLFolderSuffix is required."),
    [string]
    $SQLExcludePattern = $(throw "-SQLExcludePattern is required.")
)

$FullSQLFolderPath = "$SQLFilePath\$SQLFolderSuffix";

Get-ChildItem $FullSQLFolderPath -Recurse -Filter *.sql -Exclude $SQLExcludePattern | 

Foreach-Object {
    $scriptfullpath = $_.FullName
    $scriptname = $_.Name

    Try
    {
        Invoke-Sqlcmd -ConnectionString $ConnectionString -InputFile $scriptfullpath -ErrorAction Stop
        Write-Host "[Completed] $scriptname"
    }
    Catch
    {
        $ErrorMessage = $_.Exception.Message
        Write-Error "[Error running $scriptname]: $ErrorMessage"
        Exit 1
    }
}