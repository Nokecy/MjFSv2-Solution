-- MjFS Database version 4 --
PRAGMA auto_vacuum = "1";

CREATE TABLE "Config" (
	`location`	TEXT NOT NULL,
	`version`	INTEGER NOT NULL,
	`hash`		TEXT
);

CREATE TABLE "Item" (
	`id`	TEXT NOT NULL,
	`name`	TEXT NOT NULL,
	`ext`	TEXT NOT NULL,
	`size`	TEXT NOT NULL,
	`attr`	TEXT NOT NULL,
	`lat`	TEXT NOT NULL,
	`lwt`	TEXT NOT NULL,
	`ct`	TEXT NOT NULL,
	PRIMARY KEY(id)
);

CREATE TABLE `ItemTag` (
	`itemId` 	TEXT NOT NULL,
	`tagId`		TEXT NOT NULL,
	PRIMARY KEY(itemId,tagId)
);

CREATE TABLE `Tag` (
	`id`	TEXT NOT NULL,
	`rootVisible`	INTEGER NOT NULL,
	PRIMARY KEY(id)
);

INSERT INTO `Config`(`location`,`version`,`hash`) VALUES (@defaultBag, @version, NULL);

INSERT INTO `Tag`(`id`,`rootVisible`) VALUES ('miscellaneous',1);
INSERT INTO `Tag`(`id`,`rootVisible`) VALUES ('document',1);
INSERT INTO `Tag`(`id`,`rootVisible`) VALUES ('picture',1);
INSERT INTO `Tag`(`id`,`rootVisible`) VALUES ('music',1);
INSERT INTO `Tag`(`id`,`rootVisible`) VALUES ('video',1);


