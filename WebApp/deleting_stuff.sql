SET SQL_SAFE_UPDATES = 0;
Delete from Subscriptions

Select * from Subscriptions

Select notifications from Aspnetusers

UPDATE AspNetUsers
SET notifications = Null

delete from friends

Select * from friends

-- Insert a row with UserId and FriendId having the status 'follow'
INSERT INTO Friends (UserId, FriendId, Status)
VALUES ('08dca7a8-14eb-42ae-8f42-8ae1e64151d3', '08dcac52-ca7f-42f9-83b6-36ff5c484d56', 'followed');

-- Insert a row with UserId and FriendId having the status 'followed'
INSERT INTO Friends (UserId, FriendId, Status)
VALUES ('08dcac52-ca7f-42f9-83b6-36ff5c484d56', '08dca7a8-14eb-42ae-8f42-8ae1e64151d3', 'follow');

ALTER TABLE Friends DROP PRIMARY KEY;

ALTER TABLE friends DROP FOREIGN KEY FK_Friends_AspNetUsers_UserId
