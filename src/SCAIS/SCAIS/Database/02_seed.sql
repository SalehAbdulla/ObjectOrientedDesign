/* =====================================================================
   SCAIS - Smart Course Advising Information System
   02_seed.sql - mock data

   Every table receives at least 15 records, as required by the project
   brief. Explicit identity values are used so that every foreign key in
   this script is readable and self-documenting.

   Passwords are stored as SHA-256 hashes, never as plain text. The hash
   expression is exactly the same algorithm as PasswordHasher.Hash() in
   the application:
       SHA2_256( password + '|SCAIS' ) rendered as uppercase hex.
   Demonstrator credentials:
       administrator : admin     / Admin@123
       adviser       : adviser01 / Adviser@123
       student       : student01 / Student@123
   ===================================================================== */

/* ---------------------------------------------------------------
   Specializations - 15 records.
   Rows 1-4 are the four flagship tracks named in the business case.
   Rows 5-15 are additional tracks available for future expansion.
   --------------------------------------------------------------- */
SET IDENTITY_INSERT dbo.Specializations ON;

INSERT INTO dbo.Specializations (SpecializationId, Name, Description) VALUES
 (1,  N'Programming',                   N'Software construction, algorithms and object oriented design.'),
 (2,  N'Networking',                    N'Network infrastructure, routing, switching and wireless communication.'),
 (3,  N'Cybersecurity',                 N'Defensive and offensive security, threat modelling and incident response.'),
 (4,  N'Database',                      N'Relational design, administration, tuning and business intelligence.'),
 (5,  N'Software Engineering',          N'Requirements engineering, architecture and quality assurance.'),
 (6,  N'Artificial Intelligence',       N'Machine learning, knowledge representation and intelligent agents.'),
 (7,  N'Data Science',                  N'Statistical analysis, data mining and predictive modelling.'),
 (8,  N'Cloud Computing',               N'Cloud architecture, virtualisation and distributed services.'),
 (9,  N'Internet of Things',            N'Embedded devices, sensor networks and edge computing.'),
 (10, N'Game Development',              N'Game engines, real-time rendering and interactive design.'),
 (11, N'Mobile Application Development',N'Native and cross platform mobile application engineering.'),
 (12, N'Information Security',          N'Governance, risk, compliance and security auditing.'),
 (13, N'Business Intelligence',         N'Data warehousing, reporting and decision support systems.'),
 (14, N'Robotics',                      N'Control systems, automation and autonomous agents.'),
 (15, N'Virtual Reality',               N'Immersive interaction, 3D simulation and spatial computing.');

SET IDENTITY_INSERT dbo.Specializations OFF;
GO

/* ---------------------------------------------------------------
   Users - 40 records.
   UserIds  1-2   administrators
   UserIds  3-10  advisers
   UserIds 11-40  students
   --------------------------------------------------------------- */
SET IDENTITY_INSERT dbo.Users ON;

INSERT INTO dbo.Users (UserId, Username, PasswordHash, FullName, Email, Role, IsActive) VALUES
 (1,  N'admin',     CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Admin@123|SCAIS'),   2), N'System Administrator',      N'admin@bit.edu.bh',        N'Administrator', 1),
 (2,  N'admin2',    CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Admin@123|SCAIS'),   2), N'Registrar Administrator',   N'registrar@bit.edu.bh',    N'Administrator', 1),
 (3,  N'adviser01', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Adviser@123|SCAIS'), 2), N'Dr. Fatima Al Zayani',      N'f.alzayani@bit.edu.bh',   N'Adviser',       1),
 (4,  N'adviser02', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Adviser@123|SCAIS'), 2), N'Dr. Ahmed Al Khalifa',      N'a.alkhalifa@bit.edu.bh',  N'Adviser',       1),
 (5,  N'adviser03', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Adviser@123|SCAIS'), 2), N'Dr. Mariam Al Doseri',      N'm.aldoseri@bit.edu.bh',   N'Adviser',       1),
 (6,  N'adviser04', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Adviser@123|SCAIS'), 2), N'Dr. Hassan Al Mannai',      N'h.almannai@bit.edu.bh',  N'Adviser',       1),
 (7,  N'adviser05', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Adviser@123|SCAIS'), 2), N'Dr. Noor Al Saffar',        N'n.alsaffar@bit.edu.bh',   N'Adviser',       1),
 (8,  N'adviser06', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Adviser@123|SCAIS'), 2), N'Dr. Yousef Al Bin Ali',     N'y.albinmohamed@bit.edu.bh', N'Adviser',     1),
 (9,  N'adviser07', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Adviser@123|SCAIS'), 2), N'Dr. Layla Al Qassab',       N'l.alqassab@bit.edu.bh',   N'Adviser',       1),
 (10, N'adviser08', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Adviser@123|SCAIS'), 2), N'Dr. Omar Al Sayed',         N'o.alsayed@bit.edu.bh',    N'Adviser',       1);

SET IDENTITY_INSERT dbo.Users OFF;

/* ---------------------------------------------------------------
   Users (continued) - 30 student accounts, UserIds 11-40
   --------------------------------------------------------------- */
SET IDENTITY_INSERT dbo.Users ON;

INSERT INTO dbo.Users (UserId, Username, PasswordHash, FullName, Email, Role, IsActive) VALUES
 (11, N'student01', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Ali Hasan Ali',            N'ali.hasan@student.bit.edu.bh',       N'Student', 1),
 (12, N'student02', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Sara Ahmed Jassim',        N'sara.ahmed@student.bit.edu.bh',      N'Student', 1),
 (13, N'student03', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Mohammed Salman',          N'mohammed.salman@student.bit.edu.bh', N'Student', 1),
 (14, N'student04', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Noora Abdulla',            N'noora.abdulla@student.bit.edu.bh',   N'Student', 1),
 (15, N'student05', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Yousif Ibrahim',           N'yousif.ibrahim@student.bit.edu.bh',  N'Student', 1),
 (16, N'student06', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Zainab Mansoor',           N'zainab.mansoor@student.bit.edu.bh',  N'Student', 1),
 (17, N'student07', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Abdulla Rashid',           N'abdulla.rashid@student.bit.edu.bh',  N'Student', 1),
 (18, N'student08', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Huda Khalil',              N'huda.khalil@student.bit.edu.bh',     N'Student', 1),
 (19, N'student09', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Faisal Nasser',            N'faisal.nasser@student.bit.edu.bh',   N'Student', 1),
 (20, N'student10', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Amal Saad',                N'amal.saad@student.bit.edu.bh',       N'Student', 1),
 (21, N'student11', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Jassim Al Aradi',          N'jassim.alaradi@student.bit.edu.bh',  N'Student', 1),
 (22, N'student12', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Maryam Fahad',             N'maryam.fahad@student.bit.edu.bh',    N'Student', 1),
 (23, N'student13', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Sayed Mahdi',              N'sayed.mahdi@student.bit.edu.bh',     N'Student', 1),
 (24, N'student14', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Lulwa Yaqoob',             N'lulwa.yaqoob@student.bit.edu.bh',    N'Student', 1),
 (25, N'student15', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Khalid Bukhari',           N'khalid.bukhari@student.bit.edu.bh',  N'Student', 1),
 (26, N'student16', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Dana Al Dosari',           N'dana.aldosari@student.bit.edu.bh',   N'Student', 1),
 (27, N'student17', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Rashed Al Nuaimi',         N'rashed.alnuaimi@student.bit.edu.bh', N'Student', 1),
 (28, N'student18', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Fatema Janahi',            N'fatema.janahi@student.bit.edu.bh',   N'Student', 1),
 (29, N'student19', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Isa Al Sada',              N'isa.alsada@student.bit.edu.bh',      N'Student', 1),
 (30, N'student20', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Batool Ebrahim',           N'batool.ebrahim@student.bit.edu.bh',  N'Student', 1),
 (31, N'student21', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Salman Al Kaabi',          N'salman.alkaabi@student.bit.edu.bh',  N'Student', 1),
 (32, N'student22', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Aisha Al Binali',          N'aisha.albinali@student.bit.edu.bh',  N'Student', 1),
 (33, N'student23', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Talal Al Marzooq',         N'talal.almarzooq@student.bit.edu.bh', N'Student', 1),
 (34, N'student24', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Shaima Al Ali',            N'shaima.alali@student.bit.edu.bh',    N'Student', 1),
 (35, N'student25', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Adel Habib',               N'adel.habib@student.bit.edu.bh',      N'Student', 1),
 (36, N'student26', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Wafa Al Saeed',            N'wafa.alsaeed@student.bit.edu.bh',    N'Student', 1),
 (37, N'student27', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Husain Ali Radhi',         N'husain.radhi@student.bit.edu.bh',    N'Student', 1),
 (38, N'student28', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Reem Al Mannai',           N'reem.almannai@student.bit.edu.bh',   N'Student', 1),
 (39, N'student29', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Ghanim Al Kuwari',         N'ghanim.alkuwari@student.bit.edu.bh', N'Student', 1),
 (40, N'student30', CONVERT(NVARCHAR(128), HASHBYTES('SHA2_256', 'Student@123|SCAIS'), 2), N'Marwa Al Saffar',          N'marwa.alsaffar@student.bit.edu.bh',  N'Student', 1);

SET IDENTITY_INSERT dbo.Users OFF;
GO

/* ---------------------------------------------------------------
   Administrators - 15 records.
   Administrator 1 and 2 own real user accounts (UserIds 1 and 2);
   the remaining 13 represent administrative staff records kept for
   completeness of the mock data set.
   --------------------------------------------------------------- */
SET IDENTITY_INSERT dbo.Administrators ON;

INSERT INTO dbo.Administrators (AdminId, UserId, AccessLevel) VALUES
 (1,  1,  N'Full'),
 (2,  2,  N'Full'),
 (3,  3,  N'Registrar'),
 (4,  4,  N'Registrar'),
 (5,  5,  N'Catalog'),
 (6,  6,  N'Catalog'),
 (7,  7,  N'Reporting'),
 (8,  8,  N'Reporting'),
 (9,  9,  N'ReadOnly'),
 (10, 10, N'ReadOnly'),
 (11, 11, N'ReadOnly'),
 (12, 12, N'ReadOnly'),
 (13, 13, N'ReadOnly'),
 (14, 14, N'ReadOnly'),
 (15, 15, N'ReadOnly');

SET IDENTITY_INSERT dbo.Administrators OFF;
GO

/* ---------------------------------------------------------------
   Advisers - 15 records.
   AdviserIds 1-8 map to the eight adviser user accounts (UserIds
   3-10). AdviserIds 9-15 are additional faculty records.
   --------------------------------------------------------------- */
SET IDENTITY_INSERT dbo.Advisers ON;

INSERT INTO dbo.Advisers (AdviserId, UserId, Department, Office) VALUES
 (1,  3,  N'Information Technology', N'B-201'),
 (2,  4,  N'Information Technology', N'B-202'),
 (3,  5,  N'Information Technology', N'B-203'),
 (4,  6,  N'Information Technology', N'B-204'),
 (5,  7,  N'Computer Science',       N'B-205'),
 (6,  8,  N'Computer Science',       N'B-206'),
 (7,  9,  N'Computer Science',       N'B-207'),
 (8,  10, N'Information Technology', N'B-208'),
 (9,  16, N'Computer Science',       N'C-101'),
 (10, 17, N'Computer Science',       N'C-102'),
 (11, 18, N'Information Technology', N'C-103'),
 (12, 19, N'Information Technology', N'C-104'),
 (13, 20, N'Mathematics',            N'C-105'),
 (14, 21, N'Mathematics',            N'C-106'),
 (15, 22, N'Business Computing',     N'C-107');

SET IDENTITY_INSERT dbo.Advisers OFF;
GO

/* ---------------------------------------------------------------
   Students - 30 records.
   StudentIds 1-30 map to the thirty student user accounts
   (UserIds 11-40). SpecializationIds 1-4 are the four flagship
   tracks; AdviserIds 1-8 are the eight active faculty advisers.
   --------------------------------------------------------------- */
SET IDENTITY_INSERT dbo.Students ON;

INSERT INTO dbo.Students (StudentId, UserId, SpecializationId, AdviserId, EnrollmentYear, GPA) VALUES
 (1,  11, 1, 1,    2023, 3.42), (2,  12, 2, 2,    2023, 3.15),
 (3,  13, 3, 3,    2023, 2.88), (4,  14, 4, 4,    2023, 3.67),
 (5,  15, 1, 1,    2023, 2.95), (6,  16, 2, 2,    2023, 3.80),
 (7,  17, 3, 3,    2023, 3.05), (8,  18, 4, 4,    2023, 3.55),
 (9,  19, 1, 5,    2024, 3.22), (10, 20, 2, 6,    2024, 2.74),
 (11, 21, 3, 7,    2024, 3.91), (12, 22, 4, 8,    2024, 3.38),
 (13, 23, 1, 1,    2024, 2.60), (14, 24, 2, 2,    2024, 3.44),
 (15, 25, 3, 3,    2024, 3.72), (16, 26, 4, 4,    2024, 2.85),
 (17, 27, 1, 5,    2024, 3.10), (18, 28, 2, 6,    2024, 3.63),
 (19, 29, 3, 7,    2025, 3.28), (20, 30, 4, 8,    2025, 2.91),
 (21, 31, 1, 1,    2025, 3.50), (22, 32, 2, 2,    2025, 3.06),
 (23, 33, 3, 3,    2025, 2.77), (24, 34, 4, 4,    2025, 3.85),
 (25, 35, 1, 5,    2025, 3.33), (26, 36, 2, 6,    2025, 2.98),
 (27, 37, 3, 7,    2025, 3.59), (28, 38, 4, 8,    2025, 3.17),
 (29, 39, 1, NULL, 2025, NULL), (30, 40, 2, NULL, 2025, NULL);

SET IDENTITY_INSERT dbo.Students OFF;
GO

/* ---------------------------------------------------------------
   Courses - 30 records.
   CourseIds 1-8 are core subjects (IsCore = 1, no specialization).
   CourseIds 9-30 are specialised subjects belonging to tracks 1-4.
   --------------------------------------------------------------- */
SET IDENTITY_INSERT dbo.Courses ON;

INSERT INTO dbo.Courses (CourseId, Code, Title, CreditHours, IsCore, SpecializationId, Description) VALUES
 (1,  N'IT101', N'Computer Systems',                  3, 1, NULL, N'Number systems, hardware organisation and operating system fundamentals.'),
 (2,  N'IT102', N'Math for Computing',                3, 1, NULL, N'Discrete mathematics and logic for computing.'),
 (3,  N'IT103', N'Unix System',                       3, 1, NULL, N'Shell scripting, file systems and process management.'),
 (4,  N'IT104', N'Programming Fundamentals',          4, 1, NULL, N'Variables, control flow, methods and arrays in C#.'),
 (5,  N'IT105', N'Data Structures',                   4, 1, NULL, N'Lists, stacks, queues, trees and hashing.'),
 (6,  N'IT106', N'Database 1',                        3, 1, NULL, N'Relational modelling, normalisation and SQL.'),
 (7,  N'IT107', N'Computer Networks',                 3, 1, NULL, N'OSI and TCP/IP models, addressing and subnetting.'),
 (8,  N'IT108', N'Academic English',                  2, 1, NULL, N'Technical writing and presentation skills.'),
 (9,  N'IT210', N'Object Oriented Design',            4, 0, 1,    N'Encapsulation, inheritance, polymorphism and design patterns.'),
 (10, N'IT211', N'Advanced Programming',              4, 0, 1,    N'Generics, LINQ, delegates and asynchronous programming.'),
 (11, N'IT212', N'Software Testing',                  3, 0, 1,    N'Unit testing, test driven development and defect tracking.'),
 (12, N'IT213', N'Web Application Development',       3, 0, 1,    N'Server side web development and REST services.'),
 (13, N'IT214', N'Algorithms and Complexity',         4, 0, 1,    N'Sorting, searching, graph algorithms and complexity analysis.'),
 (14, N'IT220', N'Advanced Networking',               4, 0, 2,    N'Routing protocols, VLANs and wide area networking.'),
 (15, N'IT221', N'Networking and Data Communications 2', 4, 0, 2, N'Advanced data communications and network services.'),
 (16, N'IT222', N'Wireless Communication',            3, 0, 2,    N'Radio propagation, 802.11 and mobile networks.'),
 (17, N'IT223', N'Network Security',                  3, 0, 2,    N'Firewalls, VPNs, intrusion detection and hardening.'),
 (18, N'IT224', N'Network Administration',            3, 0, 2,    N'Directory services, monitoring and network automation.'),
 (19, N'IT230', N'Defense in Depth',                  3, 0, 3,    N'Layered security architecture and controls.'),
 (20, N'IT231', N'Ethical Hacking',                   4, 0, 3,    N'Vulnerability assessment and penetration testing.'),
 (21, N'IT232', N'Digital Forensics',                 3, 0, 3,    N'Evidence acquisition, preservation and analysis.'),
 (22, N'IT233', N'Cryptography',                      4, 0, 3,    N'Symmetric and asymmetric cryptography and PKI.'),
 (23, N'IT234', N'Security Governance',               3, 0, 3,    N'Risk management, policy and compliance frameworks.'),
 (24, N'IT240', N'Database 2',                        4, 0, 4,    N'Advanced SQL, indexing, transactions and tuning.'),
 (25, N'IT241', N'Database Administration',           4, 0, 4,    N'Backup, recovery, security and performance management.'),
 (26, N'IT242', N'Data Warehousing',                  3, 0, 4,    N'Star and snowflake schemas, ETL and OLAP.'),
 (27, N'IT243', N'Business Intelligence',             3, 0, 4,    N'Reporting, dashboards and decision support.'),
 (28, N'IT244', N'NoSQL Databases',                   3, 0, 4,    N'Document, key value and graph data stores.'),
 (29, N'IT150', N'Professional Practice',             2, 1, NULL, N'Teamwork, ethics and professional responsibility.'),
 (30, N'IT151', N'Project Management',                3, 1, NULL, N'Planning, scheduling, risk and stakeholder management.');

SET IDENTITY_INSERT dbo.Courses OFF;
GO

/* ---------------------------------------------------------------
   Prerequisites - 22 records (20 prerequisite rules and 2 corequisite
   rules). These include the two examples given in the business case:
      Database 1                        -> Database 2
      Advanced Networking               -> Networking and Data
                                           Communications 2
   The rules are what EligibilityValidator enforces at enrolment time.
   --------------------------------------------------------------- */
INSERT INTO dbo.Prerequisites (CourseId, RequiredCourseId, RuleType) VALUES
 -- Database track
 (24, 6,  N'Prerequisite'),   -- Database 2 requires Database 1
 (25, 24, N'Prerequisite'),   -- Database Administration requires Database 2
 (26, 24, N'Prerequisite'),   -- Data Warehousing requires Database 2
 (28, 24, N'Prerequisite'),   -- NoSQL Databases requires Database 2
 (27, 26, N'Prerequisite'),   -- Business Intelligence requires Data Warehousing
 -- Networking track
 (14, 3,  N'Prerequisite'),   -- Advanced Networking requires Unix System
 (14, 7,  N'Prerequisite'),   -- Advanced Networking requires Computer Networks
 (15, 14, N'Prerequisite'),   -- Networking and Data Communications 2 requires Advanced Networking
 (16, 14, N'Prerequisite'),   -- Wireless Communication requires Advanced Networking
 (17, 14, N'Prerequisite'),   -- Network Security requires Advanced Networking
 (18, 14, N'Prerequisite'),   -- Network Administration requires Advanced Networking
 -- Cybersecurity track
 (20, 17, N'Prerequisite'),   -- Ethical Hacking requires Network Security
 (21, 17, N'Prerequisite'),   -- Digital Forensics requires Network Security
 (22, 2,  N'Prerequisite'),   -- Cryptography requires Math for Computing
 (23, 19, N'Prerequisite'),   -- Security Governance requires Defense in Depth
 -- Programming track
 (9,  2,  N'Prerequisite'),   -- Object Oriented Design requires Math for Computing
 (9,  4,  N'Prerequisite'),   -- Object Oriented Design requires Programming Fundamentals
 (10, 9,  N'Prerequisite'),   -- Advanced Programming requires Object Oriented Design
 (11, 9,  N'Prerequisite'),   -- Software Testing requires Object Oriented Design
 (13, 5,  N'Prerequisite'),   -- Algorithms and Complexity requires Data Structures
 -- Corequisites: may be taken in the same semester as the dependent course
 (5,  4,  N'Corequisite'),    -- Data Structures with Programming Fundamentals
 (26, 27, N'Corequisite');    -- Data Warehousing with Business Intelligence

/* ---------------------------------------------------------------
   Semesters - 15 records (three terms across five academic years)
   SemesterId 10 (Semester 1, 2026) is treated as the current term by
   the application when it recommends courses.
   --------------------------------------------------------------- */
SET IDENTITY_INSERT dbo.Semesters ON;

INSERT INTO dbo.Semesters (SemesterId, TermName, AcademicYear, StartDate, EndDate) VALUES
 (1,  N'Semester 1',     2023, '2023-09-03', '2023-12-21'),
 (2,  N'Semester 2',     2023, '2024-01-14', '2024-05-02'),
 (3,  N'Summer Semester',2023, '2024-05-26', '2024-07-11'),
 (4,  N'Semester 1',     2024, '2024-09-01', '2024-12-19'),
 (5,  N'Semester 2',     2024, '2025-01-12', '2025-04-30'),
 (6,  N'Summer Semester',2024, '2025-05-25', '2025-07-10'),
 (7,  N'Semester 1',     2025, '2025-09-07', '2025-12-18'),
 (8,  N'Semester 2',     2025, '2026-01-11', '2026-04-29'),
 (9,  N'Summer Semester',2025, '2026-05-24', '2026-07-09'),
 (10, N'Semester 1',     2026, '2026-09-06', '2026-12-17'),
 (11, N'Semester 2',     2026, '2027-01-10', '2027-04-28'),
 (12, N'Summer Semester',2026, '2027-05-23', '2027-07-08'),
 (13, N'Semester 1',     2027, '2027-09-05', '2027-12-16'),
 (14, N'Semester 2',     2027, '2028-01-09', '2028-04-27'),
 (15, N'Summer Semester',2027, '2028-05-21', '2028-07-06');

SET IDENTITY_INSERT dbo.Semesters OFF;
GO

/* ---------------------------------------------------------------
   CourseOfferings - 40 records.
   Every catalogue course is offered in the current semester
   (SemesterId 10) and the first ten are also offered next semester.
   --------------------------------------------------------------- */
INSERT INTO dbo.CourseOfferings (CourseId, SemesterId, Capacity)
SELECT c.CourseId, 10, 30
FROM dbo.Courses AS c
WHERE c.CourseId BETWEEN 1 AND 30;

INSERT INTO dbo.CourseOfferings (CourseId, SemesterId, Capacity)
SELECT c.CourseId, 11, 25
FROM dbo.Courses AS c
WHERE c.CourseId BETWEEN 1 AND 10;

GO

/* ---------------------------------------------------------------
   Enrollments - 160 records, covering all three course statuses so
   that the "track course status" requirement is demonstrable.

     90 records : Completed  (core subjects 1-3, semester 1)
     30 records : InProgress (core subject 4, current semester 10)
     30 records : Pending    (core subject 5, current semester 10)
     10 records : Completed  (Object Oriented Design, course 9)

   The last group matters: ten students have completed IT210 Object
   Oriented Design and are therefore eligible for IT211 Advanced
   Programming, while the other twenty have not and are blocked by
   PrerequisiteRule. That difference is what the demo video shows.
   --------------------------------------------------------------- */
INSERT INTO dbo.Enrollments (StudentId, CourseId, SemesterId, Status, Grade)
SELECT s.StudentId,
       c.CourseId,
       1,
       'Completed',
       CASE (s.StudentId + c.CourseId) % 5
            WHEN 0 THEN 'A'
            WHEN 1 THEN 'B+'
            WHEN 2 THEN 'B'
            WHEN 3 THEN 'C+'
            ELSE 'A-'
       END
FROM dbo.Students AS s
CROSS JOIN dbo.Courses AS c
WHERE c.CourseId BETWEEN 1 AND 3;

INSERT INTO dbo.Enrollments (StudentId, CourseId, SemesterId, Status, Grade)
SELECT s.StudentId, 4, 10, 'InProgress', NULL
FROM dbo.Students AS s;

INSERT INTO dbo.Enrollments (StudentId, CourseId, SemesterId, Status, Grade)
SELECT s.StudentId, 5, 10, 'Pending', NULL
FROM dbo.Students AS s;

INSERT INTO dbo.Enrollments (StudentId, CourseId, SemesterId, Status, Grade) VALUES
 (1,  9, 7, 'Completed', 'A'),  (3,  9, 7, 'Completed', 'B+'),
 (5,  9, 7, 'Completed', 'A'),  (7,  9, 7, 'Completed', 'B'),
 (9,  9, 7, 'Completed', 'A'),  (11, 9, 7, 'Completed', 'B+'),
 (13, 9, 7, 'Completed', 'C+'), (15, 9, 7, 'Completed', 'A'),
 (17, 9, 7, 'Completed', 'B'),  (19, 9, 7, 'Completed', 'B+');

GO

/* ---------------------------------------------------------------
   CourseRequests - 20 records covering all four request states.
     Requests  1- 6 : Approved
     Requests  7-10 : Rejected
     Requests 11-16 : Submitted (awaiting the adviser)
     Requests 17-20 : Draft
   AdviserId matches the adviser assigned to the student in the
   Students table.
   --------------------------------------------------------------- */
SET IDENTITY_INSERT dbo.CourseRequests ON;

INSERT INTO dbo.CourseRequests (RequestId, StudentId, AdviserId, SemesterId, Status, SubmittedDate, AdviserRemark) VALUES
 (1,  1,  1, 10, N'Approved',  '2026-08-20 09:15:00', N'Plan approved. Registration confirmed.'),
 (2,  2,  2, 10, N'Approved',  '2026-08-20 10:02:00', N'Approved as submitted.'),
 (3,  3,  3, 10, N'Approved',  '2026-08-21 08:44:00', N'Approved. Keep the cryptography elective.'),
 (4,  4,  4, 10, N'Approved',  '2026-08-21 11:30:00', N'Approved. Database track on schedule.'),
 (5,  5,  1, 10, N'Approved',  '2026-08-22 13:05:00', N'Approved after removing one elective.'),
 (6,  6,  2, 10, N'Approved',  '2026-08-22 15:47:00', N'Approved. Well balanced load.'),
 (7,  7,  3, 10, N'Rejected',  '2026-08-23 09:20:00', N'Rejected: prerequisite for Network Security is not yet complete.'),
 (8,  8,  4, 10, N'Rejected',  '2026-08-23 10:11:00', N'Rejected: exceeds the 18 credit hour limit for the semester.'),
 (9,  9,  5, 10, N'Rejected',  '2026-08-24 08:59:00', N'Rejected: please resubmit with Software Testing included.'),
 (10, 10, 6, 10, N'Rejected',  '2026-08-24 14:36:00', N'Rejected: Advanced Networking must be completed first.'),
 (11, 11, 7, 10, N'Submitted', '2026-09-01 09:00:00', NULL),
 (12, 12, 8, 10, N'Submitted', '2026-09-01 09:12:00', NULL),
 (13, 13, 1, 10, N'Submitted', '2026-09-02 10:25:00', NULL),
 (14, 14, 2, 10, N'Submitted', '2026-09-02 11:41:00', NULL),
 (15, 15, 3, 10, N'Submitted', '2026-09-03 08:30:00', NULL),
 (16, 16, 4, 10, N'Submitted', '2026-09-03 16:05:00', NULL),
 (17, 17, 5, 10, N'Draft',     NULL, NULL),
 (18, 18, 6, 10, N'Draft',     NULL, NULL),
 (19, 19, 7, 10, N'Draft',     NULL, NULL),
 (20, 20, 8, 10, N'Draft',     NULL, NULL);

SET IDENTITY_INSERT dbo.CourseRequests OFF;
GO

/* ---------------------------------------------------------------
   CourseRequestLines - 45 records (two or three courses per request).
   These are the composition children of CourseRequests: deleting a
   request cascades to its lines.
   --------------------------------------------------------------- */
INSERT INTO dbo.CourseRequestLines (RequestId, CourseId, EligibilityNote) VALUES
 -- Request 1 (Programming) - 3 lines
 (1, 10, N'Prerequisite IT210 satisfied'),
 (1, 11, N'Prerequisite IT210 satisfied'),
 (1, 5,  N'Eligible'),
 -- Request 2 (Networking) - 3 lines
 (2, 14, N'Prerequisite IT103 satisfied'),
 (2, 16, N'Prerequisite IT220 satisfied'),
 (2, 7,  N'Eligible'),
 -- Request 3 (Cybersecurity) - 2 lines
 (3, 19, N'Eligible'),
 (3, 22, N'Prerequisite IT102 satisfied'),
 -- Request 4 (Database) - 2 lines
 (4, 24, N'Prerequisite IT106 satisfied'),
 (4, 26, N'Prerequisite IT240 satisfied'),
 -- Request 5 (Programming) - 3 lines
 (5, 10, N'Prerequisite IT210 satisfied'),
 (5, 12, N'Eligible'),
 (5, 13, N'Prerequisite IT105 satisfied'),
 -- Request 6 (Networking) - 3 lines
 (6, 15, N'Prerequisite IT220 satisfied'),
 (6, 17, N'Prerequisite IT220 satisfied'),
 (6, 18, N'Prerequisite IT220 satisfied'),
 -- Request 7 (Cybersecurity) - 3 lines
 (7, 20, N'Blocked: IT223 Network Security not completed'),
 (7, 21, N'Blocked: IT223 Network Security not completed'),
 (7, 23, N'Eligible'),
 -- Request 8 (Database) - 2 lines
 (8, 25, N'Prerequisite IT240 satisfied'),
 (8, 27, N'Blocked: IT242 Data Warehousing not completed'),
 -- Request 9 (Programming) - 2 lines
 (9, 10, N'Prerequisite IT210 satisfied'),
 (9, 11, N'Prerequisite IT210 satisfied'),
 -- Request 10 (Networking) - 2 lines
 (10, 14, N'Blocked: IT103 Unix System not completed'),
 (10, 18, N'Blocked: IT220 Advanced Networking not completed'),
 -- Request 11 (Cybersecurity) - 2 lines
 (11, 22, N'Prerequisite IT102 satisfied'),
 (11, 23, N'Prerequisite IT230 satisfied'),
 -- Request 12 (Database) - 2 lines
 (12, 24, N'Prerequisite IT106 satisfied'),
 (12, 28, N'Prerequisite IT240 satisfied'),
 -- Request 13 (Programming) - 2 lines
 (13, 9,  N'Prerequisite IT104 satisfied'),
 (13, 13, N'Prerequisite IT105 satisfied'),
 -- Request 14 (Networking) - 2 lines
 (14, 16, N'Prerequisite IT220 satisfied'),
 (14, 18, N'Prerequisite IT220 satisfied'),
 -- Request 15 (Cybersecurity) - 2 lines
 (15, 19, N'Eligible'),
 (15, 23, N'Prerequisite IT230 satisfied'),
 -- Request 16 (Database) - 2 lines
 (16, 26, N'Prerequisite IT240 satisfied'),
 (16, 28, N'Prerequisite IT240 satisfied'),
 -- Request 17 (Programming) - 2 lines
 (17, 10, N'Prerequisite IT210 satisfied'),
 (17, 12, N'Eligible'),
 -- Request 18 (Networking) - 2 lines
 (18, 15, N'Prerequisite IT220 satisfied'),
 (18, 17, N'Prerequisite IT220 satisfied'),
 -- Request 19 (Cybersecurity) - 2 lines
 (19, 20, N'Prerequisite IT223 satisfied'),
 (19, 22, N'Prerequisite IT102 satisfied'),
 -- Request 20 (Database) - 2 lines
 (20, 24, N'Prerequisite IT106 satisfied'),
 (20, 25, N'Prerequisite IT240 satisfied');

GO

/* ---------------------------------------------------------------
   Notifications - 25 records.
   These rows exist because the Observer participants wrote them when
   a course request changed status, which is how the Observer pattern
   becomes visible in the data as well as in the code.
     Requests  1- 6 approved  -> notify the student  (UserIds 11-16)
     Requests  7-10 rejected  -> notify the student  (UserIds 17-20)
     Requests 11-16 submitted -> notify the adviser  (UserIds 3-10)
     Plus nine welcome notifications for newly enrolled students.
   --------------------------------------------------------------- */
INSERT INTO dbo.Notifications (UserId, Message, IsRead) VALUES
 (11, N'Your course plan #1 was approved.', 1),
 (12, N'Your course plan #2 was approved.', 1),
 (13, N'Your course plan #3 was approved.', 1),
 (14, N'Your course plan #4 was approved.', 0),
 (15, N'Your course plan #5 was approved.', 0),
 (16, N'Your course plan #6 was approved.', 0),
 (17, N'Your course plan #7 was rejected. Reason: prerequisite not complete.', 0),
 (18, N'Your course plan #8 was rejected. Reason: credit limit exceeded.', 0),
 (19, N'Your course plan #9 was rejected. Reason: incomplete plan.', 0),
 (20, N'Your course plan #10 was rejected. Reason: prerequisite not complete.', 0),
 (9,  N'A course plan from Ali Hasan Ali is awaiting your review.', 0),
 (10, N'A course plan from Sara Ahmed Jassim is awaiting your review.', 0),
 (3,  N'A course plan from Mohammed Salman is awaiting your review.', 0),
 (4,  N'A course plan from Noora Abdulla is awaiting your review.', 0),
 (5,  N'A course plan from Yousif Ibrahim is awaiting your review.', 0),
 (6,  N'A course plan from Zainab Mansoor is awaiting your review.', 0),
 (31, N'Welcome to SCAIS. Your adviser has been assigned.', 1),
 (32, N'Welcome to SCAIS. Your adviser has been assigned.', 1),
 (33, N'Welcome to SCAIS. Your adviser has been assigned.', 1),
 (34, N'Welcome to SCAIS. Your adviser has been assigned.', 0),
 (35, N'Welcome to SCAIS. Your adviser has been assigned.', 0),
 (36, N'Welcome to SCAIS. Your adviser has been assigned.', 0),
 (37, N'Welcome to SCAIS. Your adviser has been assigned.', 0),
 (38, N'Welcome to SCAIS. Your adviser has been assigned.', 0),
 (39, N'Welcome to SCAIS. Your adviser has been assigned.', 0);

GO





