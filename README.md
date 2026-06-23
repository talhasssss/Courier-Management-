# Courier Management System APIs

A robust backend RESTful API built with .NET Core using the Repository Pattern and Clean Architecture principles to handle end-to-end operations for a courier and logistics management system.

## 🚀 Features

*   **Authentication & Authorization:** Secure user registration, login, and role-based access control (Admin, Courier, Customer).
*   **Package Management:** Full CRUD operations for creating, updating, assigning, and tracking packages.
*   **Real-time Tracking:** Dynamic updates for tracking statuses and delivery histories.
*   **Cost Estimation Service:** Built-in logic to compute courier delivery charges based on weight, distance, or package type.
*   **Contact Management:** Handle system feedback, inquiries, and customer support channels.

---

## 🛠️ Tech Stack

*   **Backend Framework:** .NET Core Web API (C#)
*   **Database ORM:** Entity Framework Core (EF Core)
*   **Architecture:** Data Transfer Objects (DTOs), Separation of Concerns via Controllers & Services layer.
*   **API Testing:** Built-in `.http` file profiles and Swagger UI support.

---


## 📁 Project Structure

```text
ProjectAPI/
├── Controllers/      # API Endpoints (Admin, Auth, Contact, Customer, Package, Tracking)
├── Data/             # DbContext (CtmsDbContext) and Database Configurations
├── DTOs/             # Data Transfer Objects for decoupled requests/responses
├── Models/           # Database Entities (Admin, Courier, Customer, Package, Tracking, etc.)
├── Services/         # Core Business Logic Providers (CostService, TrackingService)
├── Properties/       # Environment & launch profile configurations
└── wwwroot/          # Static frontend assets and UI templates
