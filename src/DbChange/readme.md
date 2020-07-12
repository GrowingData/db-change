# DbChange

DbChange is a simple tool for managing database schemas. DbChange deploys schame changes viausing DbUp, and also tracks the current state of the database schema using SQL Server Management Objects.

Tracking the current state of the database schema makes it easy to see how schemas have evolved over time, as the change for each database object will be reflected in its schema file, with time stamps nearly matching the time that changes were applied.
