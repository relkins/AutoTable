AutoTable (aka NoNoSql aka Resharded)
=========

Automatically create and update SQL tables based on dynamic data
(Very experimental - Don't judge me)

This is an attempt to implement an idea I left in a comment on HackerNews:
https://news.ycombinator.com/item?id=5425772

I wanted to see if I could keep many of the advantages of a relational database without knowing the schema of the data that will be stored. Specifically I wanted to have high performance sql aggregate functionality. In a way, I wanted a dynamic data warehouse that could be shared across multiple databases.

How it works
=========

This was originally designed with the idea that each customer would have its own database and instance of an Engine.

The implementation is simple. You provide the Engine with an Entry which essentially contains a Table Name and a Dictionary of column names and values. The Engine takes care of detecting if this is the first time an Entry with that Table Name has been received. If it is, then then Engine performs a Create Table. If the Table already exists but there is a new column on the Entry then the Engine performs an Alter Add Column for each one. It is also possible to update a previously stored Entry.

What's Next
=========

The next thing I want to try is automatically handling child Entries. The Dictionary which contains the column names and values could also contain another Entry. The Engine would then handle performing the additional Inserts and creating foreign key relationships.
