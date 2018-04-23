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

-- ERROR: 'hello' is not in the valid set.
insert into Person (gender)
values ('hello');

declare @someInt int;

-- OK
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

declare @randomText varchar(100);

-- ERROR: randomText could be anything
insert into Person select coalesce(@randomText, 'unknown'); 

if (@randomText = 'male') or (@randomText = 'female') or (@randomText = 'other')
begin
    -- OK. TSqlStrong knows randomText must be 'male', 'female', 'other'.
    insert into Person select @randomText; 
end;

