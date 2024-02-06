CREATE TABLE TimeTracking (
    TrackingId INT PRIMARY KEY IDENTITY,
    UserId VARCHAR(50) NOT NULL,
    PunchIn DATETIME,
    PunchOut DATETIME,
    HoursWorked AS DATEDIFF(HOUR, PunchIn, PunchOut)
);
