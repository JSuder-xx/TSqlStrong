create table Person (
    id int not null,
    firstName varchar(100),
    lastName varchar(100)
);
GO

create function PersonsWithLastName(@lastName varchar(100)) 
returns table
as
return select * from Person where lastName like @lastName;
go

select 
    firstName
from
    PersonsWithLastName('Smith');

select 
    names.Name, e.lastName
from
    (
        select 'Bob' as Name
        union 
        select 'Jane' as Name
    ) names
    cross apply PersonsWithLastName(names.Name) e;


select 
    firstName
from
    -- ERROR: Wrong argument type
    PersonsWithLastName(1);

select 
    firstName
from
    -- ERROR: Incorrect # arguments
    PersonsWithLastName('Smith', 'John');

