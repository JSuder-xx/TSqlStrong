/** ----------------------------------------------------------------------------
--------------------------------------------------------------------------------
Inserting into tables with many columns of same/similar data types can be dangerous
business from a maintainability standpoint. This is especially true when 
performing the insert select with unions and a lot of logic. 

Given a table with ten or more int columns 
	And inserting values into that table using a complex series of unioned select queries 
When updating the logic, adding new columns, or adding a new unioned query
Then it is very easy to make a mistake on the ordering of the columns.

The maintainability problems arise out of 
* poor locality/proximity - the destination column list ends up being too far away 
  from the definition of the source data.
* ordered arguments rather than named arguments. 

To solve this problem T-SQL Strong will verify that _aliased_ columns match the 
name of the destination column. 
--------------------------------------------------------------------------------
-------------------------------------------------------------------------------- */

/* -----------------------------------------------------------------------------
Setup - We need a table with a sufificnet   
-------------------------------------------------------------------------------- */
create table Person
(
    firstName varchar(200),
    lastName varchar(200),
    AddressLine1 varchar(200),
    AddressLine2 varchar(200),
    City varchar(200),
    NumberOfPets int
)

-- OK
insert into Person (FirstName, LastName, AddressLine1, AddressLine2, City, NumberOfPets)
select
    'Bob',
    'Smith',
    '101 Happy Lane',
    '',
    'Pleasant City',
    1;
    
-- OK
insert into Person (FirstName, LastName, AddressLine1, AddressLine2, City, NumberOfPets)
select * from Person

-- ERROR
insert into Person (FirstName, LastName, AddressLine1, AddressLine2, City, NumberOfPets)
select
    'Bob',
    'Smith',
    '101 Happy Lane',
    '',
    1,
    'Pleasant City';
    
-- OK... But WRONG!!! We are clearly inserting an address into a name. This is what we wish to avoid.
insert into Person (FirstName, LastName, AddressLine1, AddressLine2, City, NumberOfPets)
select
    '101 Happy Lane',
    '',
    'Pleasant City',
    'Bob',
    'Smith',
    1;

-- ERROR: There we go! This caught our mistake. By using column aliases we were able to declare
-- to T-SQL Strong that the names should match. In this example it might look a little silly because
-- the select follows the insert immediately. However, imagine you have multiple unioned selects where
-- each consist of a several dozen lines of code. 
insert into Person (FirstName, LastName, AddressLine1, AddressLine2, City, NumberOfPets)
select
    '101 Happy Lane' as AddressLine1,
    '' as AddressLine2,
    'Pleasant City' as City,
    'Bob' as FirstName,
    'Smith' as LastName,
    1;

-- GOOD
insert into Person (FirstName, LastName, AddressLine1, AddressLine2, City, NumberOfPets)
select
    'Bob' as FirstName,
    'Smith' as LastName,
    '101 Happy Lane' as AddressLine1,
    '' as AddressLine2,
    'Pleasant City' as City,
    1 as NumberOfPets;
