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

-- Error: The Child primary key belongs to a different domain than the parent primary key
select * 
from
    Parent 
    inner join Child on Child.ID = Parent.ID;

-- Error: The child primary key belong to a different domain than the foreign key reference to parent.
select * 
from
    Child c1
    inner join Child c2 on c1.ID = c2.Parent_ID;

-- Error: Sibling_ID belongs to the domain of Child id's so cannot be compared against parent id's.
select * 
from
    Parent
    inner join Child on Child.Sibling_ID = Parent.ID;

-- OK
select * 
from
    Child c1
    inner join Child c2 on c1.parent_id = c2.parent_id
where
	c1.id <> c2.id;

-- ERROR: Whoops. Typo where c1's parent is joined to c1's parent. 
select * 
from
    Child c1
    inner join Child c2 on c1.parent_id = c1.parent_id
where
	c1.id <> c2.id;
