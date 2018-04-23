# Overview
TSqlStrong is a T-SQL type checker/verifier that will  
* Verify the correctness of T-Sql code before it executes against a database.
* Advanced type checking features include 
  * Key Column Comparison - Protects against incorrect joins.
  * Null Type Checking - Guard against run-time errors of assigning a null value to a non-null column.
  * Check Constraints As Enumerations - Ensure only valid literals are assigned or compared against check constraint columns.
  * Insert Into Select Alias Matching - Protect against positional mistakes made with insert-select where you have many columns. 
  * Verify the correctness of Temporary Table structure usage between different stored procedures.
* Flow Typing. TSqlStrong can learn more about types (refinement) by analyzing flow control (IF/ELSEIF, CASE).

# Examples
### Key Column Comparison  
```sql
create table Parent (
    id int not null, 
    constraint PK_Parent primary key clustered (id)
); 

create table Child (
    id int not null, 
    Parent_id int not null foreign key references Parent(id), 
    constraint PK_Child primary key clustered (id)
);

-- OK 
select * from Parent inner join Child on Child.Parent_ID = Parent.ID
-- Error
select * from Parent inner join Child on Child.ID = Parent.ID
```

### Null Type Checking
```sql
declare @tableWithNulls table (val int null);
declare @tableWithoutNulls table (val int not null);

-- OK
insert into @tableWithNulls
select val from @tableWithoutNulls;

-- TypeError
insert into @tableWithoutNulls
select val from @tableWithNulls;

-- OK
insert into @tableWithoutNulls
select COALESCE(val, 0) from @tableWithNulls;

-- when declaring a variable and immediately setting it to a non-null
-- TSqlStrong knows the value is not null.
declare @someInt int = 10;

-- OK. The value was initialized to not null.
insert into @tableWithoutNulls values (@someInt);

declare @someOtherInt int;

-- ERROR. TSqlStrong saw that @someInt became null.
insert into @tableWithoutNulls values (@someOtherInt);

if (@someOtherInt is not null)
begin
    -- OK. TSqlStrong knows that @someOtherInt is not null at this point thanks to the check.
    insert into @tableWithoutNulls values (@someInt);
end;
```

### Check Constraints Understood as Enumerations
#### Literal Values
```sql
create table Person
(
    gender varchar(40) 
        constraint ck_Person_Gender
        check (
            (gender = 'male')
            or (gender = 'female')
            or (gender = 'other') 
            or (gender = 'unknown')
        )
)

-- OK
insert into Person (gender)
values 
    ('female'),
    ('other'),
    ('male');

-- Type Error
insert into Person (gender)
values ('hello');
```

#### Case Determination
TSqlStrong can figure out all possible values returned by a CASE expression and can check that against your check constraint columns.
```sql
create table Person
(
    gender varchar(40) 
        constraint ck_Person_Gender
        check (
            (gender = 'male')
            or (gender = 'female')
            or (gender = 'other') 
            or (gender = 'unknown')
        )
)
GO

declare @someInt int;

-- OK
insert into Person (gender)
values
    (case 
        when @someInt is null then 'unknown'
        when @someInt > 0 then 'female'
        else 'male'                
    end);

-- Type Error due to 'apple' 
insert into Person (gender)
values
    (case 
        when @someInt is null then 'unknown'
        when @someInt > 0 then 'apple'
        else 'male'                
    end);
```

### Insert Into Select - Alias Matching
```sql
-- Type Error: 
insert into TableOfManyColumns (A, B, C, D /*,...Z*/)
select
    1 as A,
    'Hi' as B,
    13 as C,
    'Bye' as E, -- Error: Expected D in this position    
    /* ...Z */
from
    SomeOtherWideTable
```

### Temporary Table Structure Checking
```sql
create procedure dbo.SelectFirstAndLastNameFromTemp 
as
begin
    select firstName, lastName from #Temp;
end;
GO

create procedure dbo.CallsSelectFirstAndLastNameFromTemp
as
begin
    exec dbo.SelectFirstAndLastNameFromTemp; 
end;
GO

create procedure dbo.CreateWithFullNameAndExecuteIndirect
as
begin
    create table #Temp (fullName varchar(200));

    -- ERROR: When called from here the temp table does not have the correct structure.
    exec dbo.CallsSelectFirstAndLastNameFromTemp; 
end;
GO

create procedure dbo.CreateWithFirstAndLastNameAndExecuteIndirect
as
begin
    create table #Temp (firstName varchar(200), lastName varchar(200));
    
    -- OK: When called from here it is just fine.
    exec dbo.CallsSelectFirstAndLastNameFromTemp; 
end;
GO
```

### Flow Typing
#### Null Checking
```sql
create table Person (weight int not null)
GO

declare @someInt int; -- <-- NULLABLE. All variables are nullable by default.

insert into Person select @someInt; -- TYPE ERROR
insert into Person select Coalesce(@someInt, 1); -- OK. 
insert into Person select IsNull(@someInt, 1); -- OK. 

if (@someInt is not null)
begin
    insert into Person select @someInt; -- OK
end
else 
begin
    insert into Person select @someInt; -- TYPE ERROR
end;
```

#### Enumerations
```sql
create table Person
(
    gender varchar(40) 
        not null
        constraint ck_Person_Gender
        check (
            (gender = 'male')
            or (gender = 'female')
            or (gender = 'other') 
            or (gender = 'unknown')
        )
)
GO

declare @randomText varchar(100);

insert into Person select coalesce(@randomText, 'unknown'); -- TYPE ERROR

if (@randomText = 'male') or (@randomText = 'female') or (@randomText = 'other')
begin
    -- OK. TSqlStrong knows randomText must be 'male', 'female', 'other'.
    insert into Person select @randomText; 
end;
```

# Usage 
The project is currently a proof of concept. See ROADMAP for where it is heading.

* Fork or download the source from GitHub and build. TSqlStrong was developed in Visual Studio 2017 Community Edition.
* Sql text can be verified using the command line interface TSqlStrongCli. 
* Example Sql can be found in \TSqlStrongSpecifications\Examples
* A build task for Visual Studio Code can be found in \TSqlStrongCli\VSCodeIntegration. 

