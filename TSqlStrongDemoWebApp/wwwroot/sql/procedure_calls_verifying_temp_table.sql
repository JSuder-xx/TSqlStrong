/** ----------------------------------------------------------------------------
--------------------------------------------------------------------------------
The combination of Stored Procedures and Temp Tables is tricky.

Given a procedure PeterProc that references a temporary table TrickyTempTable 
    And expects TrickyTempTable to have a column CalamitousCarl of type Int
    And PeterProc does not create the temporary table 
Then the expectation is that callers of PeterProc will create the table
    And there is no way to verify the validity of PeterProc individually.

Given a procedure SallyStoredProc that creates TrickyTempTable
    And subsequently calls PeterProc
Then T-SQL Strong can validate PeterProc as it is called from inside SallyStoredProc

In fact, PeterProc could be called from many different stored procedures and 
could be valid in none, some, or all of the cases. 

T-SQL Strong will note an error 
* At the point of application
* And will indicate an error _inside_ the procedure (PeterProc) with a note about
  the caller that created the problem.

Experiment with the code below to get a better idea.
--------------------------------------------------------------------------------
-------------------------------------------------------------------------------- */

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
