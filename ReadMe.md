# MjFSv2

MjFSv2 is a continuation of the MjFileSystem project, updated to work with the latest Dokan-Net rlease (1.1.0.0-rc3).
MjFileSystem is a user-mode file system based on relational database technology. 

## The idea
Ever had trouble finding back your files? Trouble remembering in what exact folder you stored your photos? MjFS tries to get rid of the traditional folder as you now it. Instead of grouping files in folders, this project aims to group files by tag. Just throw all your files in some large folder. MjFS will do the rest. MjFS will index all files in the folder and will serve them back to you when requested. Folders are replaced by virtual folders; these folders only exist on-demand and the items contained in them 
are queried from the database. These virtual folders can be created by forming queries using tags. Each file can have multiple tags. A query on the MjFS volume can be as simple as browsing to "E:\pictures", given that the MjFS volume is mounted on drive E, this will generate a virtual folder with all files tagged as 'picture'. More complex queries are also possible. You can use as many tags as you like, e.g. "E:\documents\2015\work" will yield all files with the given three tags.

## Implemented so far
- Database schema for storing items (files) and their tags
- Read-only MjFS volume
- MjFS Watcher companion app to create/remove MjFS bags

## To be implemented
- Easy file tagging system
- Write functionality on the MjFS volume