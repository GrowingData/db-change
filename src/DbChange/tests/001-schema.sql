CREATE TABLE greeting (
    greeting_id INT NOT NULL IDENTITY(1,1),
    name NVARCHAR(MAX) NULL,
    language NVARCHAR(100) NULL,
    CONSTRAINT pk_greeting PRIMARY KEY (greeting_id)
);

