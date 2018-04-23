with Recurse (Num)
as
(
    select 0

    union all

    -- ERROR: Num is an Int from the base case
    select Num + 'apples' 
    from Recurse 
)
select top 10 Num from Recurse;

with Recurse (Num)
as
(
    select 0

    union all

    -- ERROR: NumX is not in the list of Recurse
    select NumX
    from Recurse 
)
select top 10 Num from Recurse;

-- ERROR: Column count mismatch between CTE and union.
with Recurse (Num)
as
(
    select 0, 1

    union all

    select Num, 20
    from Recurse 
)
select top 10 Num from Recurse;

-- GOOD!
with Recurse (Num)
as
(
    select 0

    union all

    select Num + 1
    from Recurse 
)
select top 10 Num from Recurse;

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

