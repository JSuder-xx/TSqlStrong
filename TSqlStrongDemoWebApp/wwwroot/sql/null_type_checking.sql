/** ----------------------------------------------------------------------------
--------------------------------------------------------------------------------
Nulls have been called the billion dollar mistake. 

By default T-SQL Strong will force developers to guard against nulls by either
* Performing a conditional check (flow typing)
* Coalescing
--------------------------------------------------------------------------------
-------------------------------------------------------------------------------- */

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
