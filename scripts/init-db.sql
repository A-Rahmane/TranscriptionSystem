-- Database initialization script for Transcription Service

-- Ensure database exists
CREATE DATABASE IF NOT EXISTS transcription_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE transcription_db;

-- Create user with proper privileges (if not exists)
CREATE USER IF NOT EXISTS 'transcription_user'@'%' IDENTIFIED BY 'transcription_pass';
GRANT ALL PRIVILEGES ON transcription_db.* TO 'transcription_user'@'%';
FLUSH PRIVILEGES;

-- Database will be created by EF Core migrations
-- This script is just for initial setup

SELECT 'Database initialization completed' AS Status;
