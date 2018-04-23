## Overview ##
This assembly contains a series of poor-man's sum types (discriminated unions) of non-production quality with the goals of
* avoid null and special meaning (magic) values.
* prefer expressions over statements

Types include
* **Try** A value which either succeeded with a value or failed with a message. A real Try type would fail with an exception object. 
* **Maybe** A value which either has a value or none.
* **Unit** A single value type that can be used to normalize functional style with imperative code that returns no actual value.
 

