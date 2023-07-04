IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'datamodel')
BEGIN
    EXEC('CREATE SCHEMA datamodel;');
END
