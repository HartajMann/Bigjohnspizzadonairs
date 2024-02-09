CREATE TABLE EmployeeAvailability (
    Id INT PRIMARY KEY IDENTITY,
    EmployeeId INT NOT NULL,
    DayOfWeek INT NOT NULL,
    StartTime TIME,
    EndTime TIME,
    FOREIGN KEY (EmployeeId) REFERENCES Employees(EmployeeId)
);
