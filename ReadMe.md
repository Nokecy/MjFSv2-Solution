# MjFSv2

MjFSv2 is a continuation of the MjFileSystem project, updated to work with the latest Dokan-Net rlease (1.1.0.0-rc3).
MjFileSystem is a user-mode file system based on relational database technology. 

## The idea
Just throw all your files in some folder on your root drive. Let's say we placed all our files in C:\bag. Now MjFS is able to index all the files in this directory. All files will be saved as an Item in the database. Now, MjFS will make all your files accessible by mounting them as a seperate drive, for example E:\. On E:\ you can now find all your files sorted by category (and possibly by any Tag you gave them). All audio files you just placed in C:\bag are nicely sorted in E:\music. And all those photos you took last year? They're all available at E:\pictures\2015. Through the power of SQLite it's very easy to find any file in the **bag**. By associating Tags with your files it is extremely easy to find them.


