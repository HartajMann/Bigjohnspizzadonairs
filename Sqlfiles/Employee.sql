CREATE TABLE Employees (
    EmployeeId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    Age INT NOT NULL,
    Position NVARCHAR(50) NOT NULL,
    ContactNumber NVARCHAR(15) NOT NULL,
    EmergencyContactNumber NVARCHAR(15) NOT NULL,
    Password NVARCHAR(MAX) NOT NULL
);
