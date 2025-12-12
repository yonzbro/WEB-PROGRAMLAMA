-- Migration: Add AvailabilityHours column to Trainers table
-- Database: GYMADb
-- Run this script in SQL Server Management Studio or your database tool

USE [GYMADb];
GO

-- Add AvailabilityHours column to Trainers table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Trainers]') AND name = 'AvailabilityHours')
BEGIN
    ALTER TABLE [dbo].[Trainers]
    ADD [AvailabilityHours] NVARCHAR(100) NULL;
    PRINT 'AvailabilityHours column added successfully.';
END
ELSE
BEGIN
    PRINT 'AvailabilityHours column already exists.';
END
GO

