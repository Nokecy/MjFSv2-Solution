# MjFSv2Lib

This project includes all the re-usable classes used by the other project in the MjFS solution. What follows is a short description of the contents of each package.

## Database
#### DatabaseOperations
This class represents a connection to a SQLite database. It has a set of CRUD methods to alter Items and Tags in the database. 
## Filesystem
#### MjFileSystemOperations
MjFS implementation of the IDokanOperations interface. This is the user-mode part of the Dokany file system which is specific to MjFS. This class relies heavily on the VolumeManager which enables it to communicate to the correct database files and find files efficiently.

## Manager
#### DatabaseManager
Internal class that manages the currently open SQLite connections and the corresponding DatabaseOperations objects.

#### VolumeMountManager
Public entry point of the MjFS environment. This class is able to find configuration files for MjFS and connects volumes to database connections. Moreover, this class initializes the MjFileSystemOperations file system and mounts it.

#### SynchronizationManager
Public class used by MjFSv2Watcher. SynchronizationManager listens for any file changes in the bag and updates the database accordingly.

## Model
#### Item
Every file in a *bag* is saved as an Item in the database. An item keeps some essential information about the file it is associated with such as file size, file name and extension.

#### Tag
Every Item in the database can be tagged with a Tag. A Tag says something about the Item and Items can be queried by Tag.
Each tag has a name by which it is identified.
