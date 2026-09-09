# Registration Domain Feature 🚀

The **Registration** domain provides complete administrative master management for **Stores**, **Warehouses**, and **Users**. All operations interface directly with SQL Server's legacy `SP_Master` stored procedure using Dapper.

---

## 15 API Endpoints

I created **15 API endpoints** in total, organized across 3 controllers:

### 🏪 Store Registration (5 APIs)
| # | Method | Route | Purpose |
|---|---|---|---|
| 1 | `GET` | `/api/Registration/Store` | List all stores with their Active/Inactive status |
| 2 | `GET` | `/api/Registration/Store/dropdown` | Get active stores for form dropdown menus |
| 3 | `POST` | `/api/Registration/Store` | Create a new store (validates code & name uniqueness) |
| 4 | `PUT` | `/api/Registration/Store` | Update an existing store's code & name |
| 5 | `PATCH` | `/api/Registration/Store/{storeId}/status` | Toggle store status (Active $\leftrightarrow$ Inactive) |

---

### 🏭 Warehouse Registration (5 APIs)
| # | Method | Route | Purpose |
|---|---|---|---|
| 6 | `GET` | `/api/Registration/Warehouse` | List all warehouses with their Active/Inactive status |
| 7 | `GET` | `/api/Registration/Warehouse/dropdown` | Get active warehouses for form dropdown menus |
| 8 | `POST` | `/api/Registration/Warehouse` | Create a new warehouse (code, name, address) |
| 9 | `PUT` | `/api/Registration/Warehouse` | Update an existing warehouse |
| 10 | `PATCH` | `/api/Registration/Warehouse/{whId}/status` | Toggle warehouse status (Active $\leftrightarrow$ Inactive) |

---

### 👤 User Registration (5 APIs)
| # | Method | Route | Purpose |
|---|---|---|---|
| 11 | `GET` | `/api/Registration/User` | List users (filtered by role/store/warehouse) |
| 12 | `GET` | `/api/Registration/User/roles` | Get list of user roles (Super Admin, Store Admin, etc.) |
| 13 | `POST` | `/api/Registration/User` | Create a new user with store/warehouse association |
| 14 | `PUT` | `/api/Registration/User` | Update user credentials, role, or store/warehouse assignment |
| 15 | `PATCH` | `/api/Registration/User/{userId}/status` | Toggle user status (Active $\leftrightarrow$ Inactive) |

---

*Total: 15 complete RESTful APIs covering all CRUD operations, dropdown population, duplicate checking, and status toggles.*
