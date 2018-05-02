/** ----------------------------------------------------------------------------
Bad joins happen but that does not mean they have to make it into production
...or even your QA environment.

T-SQL can validate joins in the case where there exists a foreign key from one 
table to the primary key of another table (probably the 90% case).
-------------------------------------------------------------------------------- */
create table Parent (
    id int not null, 
    constraint PK_Parent primary key clustered (id)
); 

create table Child (
    id int not null, 
    Parent_id int not null foreign key references Parent(id), 
    Sibling_id int not null foreign key references Child(id),
    constraint PK_Child primary key clustered (id)
);

-- OK 
select * 
from
    Parent 
    inner join Child on Child.Parent_ID = Parent.ID;
    
-- OK 
select * 
from
    Parent p1
    inner join Parent p2 on p1.id = p2.id;

-- OK 
select * 
from
    Child  c1
    inner join Child c2 on c1.parent_id = c2.parent_id;

-- OK 
select * 
from
    Child  c1
    inner join Child c2 on c1.sibling_id = c2.id;

-- Type Error
select * 
from
    Parent 
    inner join Child on Child.ID = Parent_ID;

-- Type Error
select * 
from
    Child c1
    inner join Child c2 on c1.ID = c2.Parent_ID;

-- Type Error
select * 
from
    Parent
    inner join Child on Child.Sibling_ID = Parent.ID;