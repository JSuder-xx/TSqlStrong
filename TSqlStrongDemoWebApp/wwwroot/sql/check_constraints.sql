/** ----------------------------------------------------------------------------
--------------------------------------------------------------------------------
T-SQL Strong will treat check constraints that enforce a column value as being 
one of several values similar to how other programming languages treat 
enumerations. 
--------------------------------------------------------------------------------
-------------------------------------------------------------------------------- */

/* -----------------------------------------------------------------------------
Setup    
-------------------------------------------------------------------------------- */
create table Person
(
    gender varchar(40) 
        -- Add a constraint that verifies a value must be one of several by employing
        -- a series equality checks OR'd together.
        constraint ck_Person_Gender
        check (
            (gender = 'male')
            or (gender = 'female')
            or (gender = 'other') 
            or (gender = 'unknown')
        )
)

/* -----------------------------------------------------------------------------
Insert Into 
-------------------------------------------------------------------------------- */
-- OK
insert into Person (gender)
    select 'female'
    union
    select 'male';

-- ERROR
insert into Person (gender)
    select 'male'
    union
    select 'females';


/* -----------------------------------------------------------------------------
Insert Into Values    
-------------------------------------------------------------------------------- */
-- OK: The checker knows that all of the values specified will pass the check constraint.
insert into Person (gender)
values 
    ('female'),
    ('other'),
    ('male');

-- ERROR: Here the checker has identified that 'hello' is not in the valid set!
insert into Person (gender)
values 
    ('hello'),
    ('goodbye');

/* -----------------------------------------------------------------------------
Case Expressions
-------------------------------------------------------------------------------- */
-- OK
declare @someInt int;
insert into Person (gender)
values
    (case 
        when @someInt is null then 'unknown'
        when @someInt > 0 then 'female'
        else 'male'                
    end);

-- ERROR: TSqlStrong figured out that 'apple' is possible and that is not a valid member of the set.
insert into Person (gender)
values
    (case 
        when @someInt is null then 'unknown'
        when @someInt > 0 then 'apple'
        else 'male'                
    end);

/* -----------------------------------------------------------------------------
Case Expressions with Select
-------------------------------------------------------------------------------- */
declare @PersonLocal table (genderUnchecked varchar(100) not null);

-- ERROR
insert into Person (gender)
select genderUnchecked from @PersonLocal;

-- OK
insert into Person (gender)
select 
    (case 
        when pl.genderUnchecked = 'male' then 'male'
        when pl.genderUnchecked = 'female' then 'female'
        when pl.genderUnchecked = 'other' then 'other'
        else 
            'unknown'
    end)
from
    @PersonLocal pl;

-- OK: We can also use an _in_ to vouch for genderUnchecked. 
-- TRY IT: Change one of the values to be something other than what is acceptable and see what T-SQL Strong says.
insert into Person (gender)
select 
    (case 
        when pl.genderUnchecked in ('male', 'female', 'other') then pl.genderUnchecked
        else 'unknown'
    end)
from
    @PersonLocal pl;


-- OK: We can also use the WHERE clause to vouch for genderUnchecked. Once genderUnchecked has been checked in the WHERE
-- clause it has a _refined_ type in the SELECT clause.
insert into Person (gender)
select 
    pl.genderUnchecked
from
    @PersonLocal pl
where
	pl.genderUnchecked in ('male', 'female', 'other');


/* -----------------------------------------------------------------------------
Refinement By If Expression
-------------------------------------------------------------------------------- */
declare @randomText varchar(100);

-- ERROR: randomText could be anything
insert into Person select coalesce(@randomText, 'unknown'); 

if (@randomText = 'male') or (@randomText = 'female') or (@randomText = 'other')
begin
    -- OK. TSqlStrong knows randomText must be 'male', 'female', 'other' to have reached this point.
    insert into Person select @randomText; 
end;

