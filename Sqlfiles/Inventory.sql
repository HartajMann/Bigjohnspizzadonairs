CREATE TABLE InventoryItems (
    InventoryId INT PRIMARY KEY IDENTITY,
    Branch VARCHAR(255),
    Name VARCHAR(255) NOT NULL,
    Description TEXT,
    Quantity INT NOT NULL,
    ExpiryDate DATE,
    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
);
