# MjFSv2

MjFSv2 is a continuation of the MjFileSystem project updated to work with the latest Dokan-Net release (1.1.0.0-rc3). MjFileSystem is a user-mode file system based on relational database technology. 

## The idea
Ever had trouble finding back your files? Trouble remembering in what exact folder you stored your photos? MjFS tries to get rid of the traditional folder as you now it. Instead of grouping files in folders, this project aims to group files by tag. Just throw all your files in some large folder. MjFS will do the rest. MjFS will index all files in the folder and will serve them back to you when requested. Folders are replaced by virtual folders; these folders only exist on-demand and the items contained in them 
are queried from the database. These virtual folders can be created by forming queries using tags. Each file can have multiple tags. A query on the MjFS volume can be as simple as browsing to "E:\pictures", given that the MjFS volume is mounted on drive E, this will generate a virtual folder with all files tagged as 'picture'. More complex queries are also possible. You can use as many tags as you like, e.g. "E:\documents\2015\work" will yield all files with the given three tags.

## Requirements
- working installation of [Dokany 1.0.0-rc3](https://github.com/dokan-dev/dokany/releases/tag/v1.0.0-RC3) 

## Libraries
I used the following NuGet packages in this project
- System.Data.sqlite
- ExifLib by Simon McKenzie
- TagLib
- DokanNET 

## System architecture
MjFS consists of a few simple building block. Each logical volume on your computer can have its own 'bag volume'. A bag volume is comprised of a configuration database (BagConfig.sqlite) and a bag location.
The bag location points to a folder used as main storage space. This is the folder in which you store all your files (without sub-directories). Via constant synchronization all the files in the bag have their own
representation in the bag volume's database. 
One or more bag volumes may be mounted to the MjFS 'main volume'. The main volume is visible as an added drive in explorer. All files stored in the mounted 
bag volumes can be accessed from the main volume. It is the main volume that enabled MjFS to generate on-demand folders based on the file information (tags) stored in each bag volume's database.

## Implemented so far
- Database schema for storing items (files) and their tags
- Read-only MjFS volume
- MjFS Watcher companion app to create/remove MjFS bags

## To be implemented
- Easy file tagging system
- Write functionality on the MjFS volume