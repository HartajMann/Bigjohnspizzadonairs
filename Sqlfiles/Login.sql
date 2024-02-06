CREATE TABLE Login (
    UserId VARCHAR(50) PRIMARY KEY,
    Password NVARCHAR(50) NOT NULL
);

INSERT INTO Login (UserId, Password) VALUES ('admin', ('123456'));