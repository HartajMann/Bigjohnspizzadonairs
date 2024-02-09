CREATE TABLE EmployeeShifts (
    ShiftId INT PRIMARY KEY IDENTITY,
    EmployeeId INT NOT NULL,
    ShiftDate DATE NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    FOREIGN KEY (EmployeeId) REFERENCES Employees(EmployeeId)
);
