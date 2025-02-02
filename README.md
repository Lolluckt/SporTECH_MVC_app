# SporTECH MVC Application

## Project Description
SporTECH MVC Application is a **web-based inventory management system for sports equipment**. The system is designed for **inventory control, order processing, and user management**.

The project is built using **ASP.NET Core MVC**, ensuring **separation of logic, UI, and data layers**.

---

## Key Features

- **Sports Equipment Management**  
  Add, edit, and delete sports equipment.  
  Track inventory availability.  
  View a list of available items.

- **Order Management**  
  Create and edit orders.  
  Filter and sort orders.  
  Track order statuses.

- **User Roles**  
  - **SuperAdmin** – Full access, manages users and orders.  
  - **Admin** – Manages sports equipment.  
  - **SalesManager** – Handles customer orders.

- **Reports & Filtering**  
  Generate sales and inventory reports.  
  Filter orders and products based on various criteria.

---

## Technology Stack

### Back-end:
- **ASP.NET Core MVC** – Web application framework
- **C#** – Programming language
- **Entity Framework Core** – ORM for database management
- **Microsoft SQL Server** – Relational database

### Front-end:
- **Razor Pages** – View templates
- **HTML, CSS, JavaScript** – UI styling and interactions

### Additional Tools:
- **LINQ** – Querying data efficiently
- **Identity Framework** – User authentication and authorization
- **Git, GitHub** – Version control system
- **Postman** – API testing tool
- **Visual Studio** – Development environment

---

## Project Structure
```plaintext
/SporTECH_MVC_app
│── /Controllers   # Controllers (handles business logic)
│── /Models        # Data models (database structure)
│── /Views         # UI Views
│── /wwwroot       # Static files (CSS, JS, images)
│── appsettings.json  # Application configuration
│── Startup.cs      # Service & middleware configuration
│── Program.cs      # Application entry point
```

## How to Run the Project?

### 1️⃣ Install Dependencies:
- **.NET SDK 6+**
- **Microsoft SQL Server**

### 2️⃣ Clone the Repository:
```bash
git clone https://github.com/Lolluckt/SporTECH_MVC_app.git
cd SporTECH_MVC_app
```
### 3️⃣ Configure the Database:
- **Update the connection string in appsettings.json.**
- **Apply database migrations**
```bash
dotnet ef database update
```
### 4️⃣ Run the Project:
```bash
dotnet run
```
The application will be available at http://localhost:5000.


