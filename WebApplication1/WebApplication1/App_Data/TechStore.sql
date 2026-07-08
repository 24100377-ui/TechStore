IF DB_ID(N'dbBTLKTPM') IS NULL
    CREATE DATABASE dbBTLKTPM;
GO
USE dbBTLKTPM;
GO

IF OBJECT_ID('dbo.Products', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products (
        Id NVARCHAR(10) NOT NULL PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Category NVARCHAR(100) NOT NULL,
        Cost INT NOT NULL DEFAULT 0,
        Price INT NOT NULL DEFAULT 0,
        Stock INT NOT NULL DEFAULT 0,
        Brand NVARCHAR(100) NULL,
        Description NVARCHAR(500) NULL,
        Emoji NVARCHAR(20) NULL
    );
END
GO

IF OBJECT_ID('dbo.Customers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers (
        Id NVARCHAR(10) NOT NULL PRIMARY KEY,
        Name NVARCHAR(150) NOT NULL,
        Phone NVARCHAR(30) NOT NULL,
        Email NVARCHAR(150) NULL,
        Address NVARCHAR(250) NULL,
        Orders INT NOT NULL DEFAULT 0,
        Spent INT NOT NULL DEFAULT 0
    );
END
GO

IF OBJECT_ID('dbo.Orders', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders (
        Id NVARCHAR(20) NOT NULL PRIMARY KEY,
        Customer NVARCHAR(150) NOT NULL,
        Product NVARCHAR(500) NOT NULL,
        Qty INT NOT NULL DEFAULT 1,
        Total INT NOT NULL DEFAULT 0,
        Status NVARCHAR(50) NOT NULL,
        OrderDate DATE NOT NULL
    );
END
GO

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        Id NVARCHAR(10) NOT NULL PRIMARY KEY,
        Username NVARCHAR(50) NOT NULL UNIQUE,
        Password NVARCHAR(100) NOT NULL,
        Fullname NVARCHAR(150) NOT NULL,
        Role NVARCHAR(30) NOT NULL,
        Status NVARCHAR(30) NOT NULL DEFAULT 'active',
        CreatedAt DATE NOT NULL DEFAULT GETDATE(),
        LastLogin DATETIME NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
    INSERT INTO dbo.Products(Id, Name, Category, Cost, Price, Stock, Brand, Description, Emoji) VALUES
    ('P001', N'iPhone 15 Pro Max', N'Điện thoại', 27000000, 33990000, 15, N'Apple', N'6.7 inch, chip A17 Pro, 48MP', N'📱'),
    ('P002', N'Samsung Galaxy S24 Ultra', N'Điện thoại', 22000000, 27990000, 8, N'Samsung', N'6.8 inch, Snapdragon 8 Gen 3', N'📱'),
    ('P003', N'MacBook Pro M3', N'Laptop', 38000000, 44990000, 5, N'Apple', N'14 inch, chip M3, 18GB RAM', N'💻'),
    ('P004', N'Dell XPS 15', N'Laptop', 30000000, 36990000, 3, N'Dell', N'15.6 inch, Intel Core Ultra 7', N'💻'),
    ('P005', N'iPad Pro M4', N'Máy tính bảng', 18000000, 22990000, 10, N'Apple', N'11 inch, chip M4, OLED', N'📟'),
    ('P006', N'AirPods Pro 2', N'Phụ kiện', 4500000, 6490000, 25, N'Apple', N'Chống ồn chủ động, USB-C', N'🎧'),
    ('P007', N'Samsung 65" QLED 4K', N'Màn hình', 18000000, 24990000, 2, N'Samsung', N'65 inch, QLED, 120Hz', N'🖥️'),
    ('P008', N'Sạc nhanh 65W GaN', N'Phụ kiện', 280000, 490000, 50, N'Baseus', N'3 cổng, tương thích đa thiết bị', N'🔌');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Customers)
BEGIN
    INSERT INTO dbo.Customers(Id, Name, Phone, Email, Address, Orders, Spent) VALUES
    ('C001', N'Nguyễn Văn An', '0901234567', 'an@gmail.com', N'Hà Nội', 3, 68470000),
    ('C002', N'Trần Thị Bình', '0912345678', 'binh@gmail.com', N'TP.HCM', 5, 112380000),
    ('C003', N'Lê Minh Châu', '0923456789', 'chau@gmail.com', N'Đà Nẵng', 2, 44980000),
    ('C004', N'Phạm Hoàng Dũng', '0934567890', 'dung@gmail.com', N'Hải Phòng', 1, 33990000);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Orders)
BEGIN
    INSERT INTO dbo.Orders(Id, Customer, Product, Qty, Total, Status, OrderDate) VALUES
    ('ORD001', N'Nguyễn Văn An', N'iPhone 15 Pro Max', 1, 33990000, N'Hoàn thành', '2025-03-01'),
    ('ORD002', N'Trần Thị Bình', N'MacBook Pro M3', 1, 44990000, N'Đang giao', '2025-03-02'),
    ('ORD003', N'Lê Minh Châu', N'Samsung Galaxy S24 Ultra', 2, 55980000, N'Chờ xử lý', '2025-03-03'),
    ('ORD004', N'Phạm Hoàng Dũng', N'iPad Pro M4', 1, 22990000, N'Hoàn thành', '2025-03-03'),
    ('ORD005', N'Nguyễn Văn An', N'AirPods Pro 2', 3, 19470000, N'Chờ xử lý', '2025-03-04'),
    ('ORD006', N'Trần Thị Bình', N'Sạc nhanh 65W GaN', 5, 2450000, N'Hoàn thành', '2025-03-04');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users)
BEGIN
    INSERT INTO dbo.Users(Id, Username, Password, Fullname, Role, Status, CreatedAt) VALUES
    ('U001', 'admin', 'admin123', N'Nguyễn Admin', 'admin', 'active', '2025-01-01'),
    ('U002', 'nhanvien', 'nv123', N'Trần Nhân Viên', 'staff', 'active', '2025-01-05'),
    ('U003', 'ketoan', 'kt123', N'Lê Kế Toán', 'accountant', 'active', '2025-01-10');
END
GO

IF OBJECT_ID('dbo.ActivityLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ActivityLogs (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Username NVARCHAR(50) NOT NULL,
        Fullname NVARCHAR(150) NULL,
        Role NVARCHAR(30) NULL,
        Action NVARCHAR(150) NOT NULL,
        Detail NVARCHAR(500) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO
