# IndyPOS

 A desktop POS application for general and hardware store

## Architecture

This solution uses Clean Architecture consisting of following layers:

- Application
- Domain
- Infrastructure
- Presentation

## Frontend

UI is a desktop application using Winforms  

## Backend

- SQLite is used as database stored locally in C:\ProgramData\IndyPOS\db
- Dapper is used as ORM
- Nokpirab is used for handling commands and queries (CQRS) 
- Prism is used as Event Aggregator for handling notifications (Pub/Sub) between frontend and backend

## Installation Guide

- An executable file (IndyPOS.Windows.Forms.exe) and its dependencies can be stored at any location, but default location is C:\ProgramData\IndyPOS\bin.

- A pre-created database file (Store.db) should be stored in C:\ProgramData\IndyPOS\db.
