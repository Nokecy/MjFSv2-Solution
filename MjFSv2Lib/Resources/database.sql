-- MjFS Database version 6 --
PRAGMA auto_vacuum = 1;
PRAGMA foreign_keys = 1;

-- Basic configuration data --
CREATE TABLE "Config" (
	location	TEXT NOT NULL,
	version	INTEGER NOT NULL,
	hash		TEXT
);

-- Registered meta tables --
CREATE TABLE MetaTables (
	tableName		TEXT PRIMARY KEY,
	friendlyName	TEXT UNIQUE,
	rootVisible		INTEGER NOT NULL,
	extends			TEXT,
	FOREIGN KEY (extends) REFERENCES MetaTables(tableName)
);

-- Meta aliases --
CREATE TABLE MetaAlias (
	alias		TEXT NOT NULL,
	colName		TEXT NOT NULL,
	tableName	TEXT NOT NULL,
	queryType	TEXT NOT NULL,
	queryStr	TEXT,
	PRIMARY KEY (alias, colName, tableName),
	FOREIGN KEY (tableName) REFERENCES MetaTables(tableName)
);



-- Default schemas --
CREATE TABLE ItemMeta (
	itemId	TEXT PRIMARY KEY,
	name	TEXT NOT NULL,
	ext		TEXT NOT NULL,
	size	TEXT NOT NULL,
	attr	TEXT NOT NULL,
	lat		TEXT NOT NULL,
	lwt		TEXT NOT NULL,
	ct		TEXT NOT NULL
);

CREATE TABLE PictureMeta (
	itemId	TEXT PRIMARY KEY,
	FOREIGN KEY (itemId) REFERENCES ItemMeta(itemId)
);

CREATE TABLE DocumentMeta (
	itemId	TEXT PRIMARY KEY,
	FOREIGN KEY (itemId) REFERENCES ItemMeta(itemId)
);

CREATE TABLE VideoMeta (
	itemId	TEXT PRIMARY KEY,
	FOREIGN KEY (itemId) REFERENCES ItemMeta(itemId)
);

CREATE TABLE MusicMeta (
	itemId	TEXT PRIMARY KEY,
	FOREIGN KEY (itemId) REFERENCES ItemMeta(itemId)
);

CREATE TABLE MiscMeta (
	itemId	TEXT PRIMARY KEY,
	FOREIGN KEY (itemId) REFERENCES ItemMeta(itemId)
);

-- TODO: Externalize meta schemas --
CREATE TABLE PictureJpegMeta (
	itemId		TEXT PRIMARY KEY,
	model		TEXT,
	iso			TEXT,
	"f-stop"	TEXT,
	artist		TEXT,
	FOREIGN KEY (itemId) REFERENCES PictureMeta(itemId)
);

CREATE TABLE MusicExtMeta (
	itemId		TEXT PRIMARY KEY,
	artist		TEXT,
	album		TEXT,
	title		TEXT,
	FOREIGN KEY (itemId) REFERENCES MusicMeta(itemId)
);

-- End of extension meta schemas --

INSERT INTO Config(location,version,hash) VALUES (@defaultBag, 7, NULL);

INSERT INTO MetaTables VALUES("ItemMeta", NULL, 0, NULL);
INSERT INTO MetaTables VALUES("PictureMeta", "Picture", 1, NULL);
INSERT INTO MetaTables VALUES("DocumentMeta", "Document", 1, NULL);
INSERT INTO MetaTables VALUES("MusicMeta", "Music", 1, NULL);
INSERT INTO MetaTables VALUES("VideoMeta", "Video", 1, NULL);
INSERT INTO MetaTables VALUES("MiscMeta", "Miscellaneous", 1, NULL);
INSERT INTO MetaTables VALUES("PictureJpegMeta", NULL, 0, "PictureMeta");
INSERT INTO MetaTables VALUES("MusicExtMeta", NULL, 0, "MusicMeta");

