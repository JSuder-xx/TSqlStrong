/** ----------------------------------------------------------------------------
--------------------------------------------------------------------------------
T-SQL Strong can validate the structure of common table expressions. 

This incudes recursive expressions. In the case of recursion the checker gets 
the type information from the base case. 
--------------------------------------------------------------------------------
-------------------------------------------------------------------------------- */

/* -----------------------------------------------------------------------------
Recursion
-------------------------------------------------------------------------------- */
-- GOOD: Everything lines up
with Recurse (Num)
as
(
    select 0

    union all

    select Num + 1
    from Recurse 
)
select top 10 
	Num 
from 
	Recurse;


with Recurse (Num)
as
(
    select 0

    union all

    -- ERROR: Num was determined to be an Int from the base case 
    -- and this cannot be added to a string.
    select Num + 'apples' 
    from Recurse 
)
select top 10 Num from Recurse;

with Recurse (Num)
as
(
    select 0

    union all

    -- ERROR: NumX is not in the list of columns of the CTE.
    select NumX
    from Recurse 
)
select top 10 Num from Recurse;


-- ERROR: Column count mismatch between CTE specification and the results of the union.
with Recurse (Num)
as
(
    select 0, 1

    union all

    select Num, 20
    from Recurse 
)
select top 10 Num from Recurse;

/* -----------------------------------------------------------------------------
Non-Recursive Examples
-------------------------------------------------------------------------------- */
with NonRecursive (Num)
as
(
    select 0
    union all
    select 1
    union all
    select 2
)
select * 
from 
    NonRecursive 
where 
    -- ERROR: TSqlStrong figured out Num can only be 0, 1, or 2 so this comparison check makes no sense.
    Num = 4;

-- GOOD
with NonRecursive (Num)
as
(
    select 0
    union all
    select 1
    union all
    select 2
)
select * 
from 
    NonRecursive 
where 
    Num = 2;

with NonRecursive (Value)
as
(
    -- OK: The alias matches
    select 1 as 'Value'
)
select top 10 * from NonRecursive;

-- ERROR: The alias 'CrazyValue' does not match the declared column name of 'Value'
with NonRecursive (Value)
as
(
    select 1 as 'CrazyValue'
)
select top 10 * from NonRecursive;
