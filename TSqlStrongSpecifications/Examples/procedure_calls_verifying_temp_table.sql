create procedure dbo.SelectFirstAndLastNameFromTemp 
as
begin
    select firstName, lastName from #Temp;
end;
GO

create procedure dbo.CallsSelectFirstAndLastNameFromTemp
AS
BEGIN
    exec dbo.SelectFirstAndLastNameFromTemp; 
END;
GO

create procedure dbo.CreateWithFullNameAndExecuteIndirect
as
begin
    create table #Temp (fullName varchar(200));

    exec dbo.CallsSelectFirstAndLastNameFromTemp; -- ERROR: When called from here the temp table does not have the correct structure.
end;
GO

create procedure dbo.CreateWithFirstAndLastNameAndExecuteIndirect
as
begin
    create table #Temp (firstName varchar(200), lastName varchar(200));

    exec dbo.CallsSelectFirstAndLastNameFromTemp; -- OK: When called from here it is just fine.
end;
GO
