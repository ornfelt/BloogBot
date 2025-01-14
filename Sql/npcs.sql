PRAGMA foreign_keys=OFF;
BEGIN TRANSACTION;
-- CREATE TABLE [Npcs](
-- 	[Name] [text] NOT NULL,
-- 	[IsInnkeeper] [bit] NOT NULL,
-- 	[SellsAmmo] [bit] NOT NULL,
-- 	[Repairs] [bit] NOT NULL,
-- 	[Quest] [bit] NOT NULL,
-- 	[Horde] [bit] NOT NULL,
-- 	[Alliance] [bit] NOT NULL,
-- 	[PositionX] [numeric](18, 0) NOT NULL,
-- 	[PositionY] [numeric](18, 0) NOT NULL,
-- 	[PositionZ] [numeric](18, 0) NOT NULL,
-- 	[Zone] [text] NOT NULL,
-- 	[Id] [integer] PRIMARY KEY AUTOINCREMENT NOT NULL
-- );
DELETE FROM Npcs WHERE `ID` IN (1, 2, 3, 4, 5, 6, 7, 8); 
INSERT INTO Npcs VALUES('Trayexir',0,0,1,0,1,0,-769.14999999999995594,-4948.5299999999998732,22.849089999999998568,'Durotar',1);
INSERT INTO Npcs VALUES('Jeena Featherbow',0,0,1,0,0,1,9821.9799999999999329,968.83099999999995333,1308.7770000000000791,'Teldrassil',2);
INSERT INTO Npcs VALUES('Archibald Kava',0,0,1,0,1,0,1859.3900000000001426,1568.8199999999998368,94.315150000000009811,'Tirisfal Glades',3);
INSERT INTO Npcs VALUES('Dermot Johns',0,0,1,0,0,1,-8897.7100000000000079,-115.32800000000000828,81.841129999999999711,'Elwynn Forest',4);
INSERT INTO Npcs VALUES('Rohok',0,0,1,0,1,0,167.27799999999999336,2795.6699999999998773,113.3652000000000104,'Hellfire Peninsula',5);
INSERT INTO Npcs VALUES('Humphry',0,0,1,0,0,1,-717.3169999999999824,2607.5800000000000089,91.012829999999986796,'Hellfire Peninsula',6);
INSERT INTO Npcs VALUES('Librarian Whitley',0,0,1,0,1,0,3646.2900000000000311,5888.9799999999992152,140.10530000000001038,'Borean Tundra',7);
INSERT INTO Npcs VALUES('Broff Bombast',0,0,1,0,0,1,2359.4699999999999562,5237.430000000000696,7.713187999999999711,'Borean Tundra',8);
COMMIT;