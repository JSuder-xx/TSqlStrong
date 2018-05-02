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
select 'female';

-- ERROR
insert into Person (gender)
select 'abc';


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
values ('hello');


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

