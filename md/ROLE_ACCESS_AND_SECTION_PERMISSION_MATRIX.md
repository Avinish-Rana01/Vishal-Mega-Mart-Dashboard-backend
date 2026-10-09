# 🛡️ Role Access & Section Permission Matrix

> **Authoritative Reference**: Single source of truth for all role-based section and menu visibility across the VMM POS Solution.  
> **Rule**: Whenever any role access or section permissions are modified in backend (`AuthService.cs`, `ControlCenterService.cs`) or frontend (`AuthContext.jsx`, `Sidebar.jsx`, `DashboardPage.jsx`), **this document must be updated immediately**.

---

## 📋 Master Role vs. Section Permission Matrix

| Section Key | Section / Sidebar Item | Super Admin | Store Admin | Store User | WH Admin | WH User | Tag Admin | Dispatch Admin |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| `live_stock` | Live Stock Tab | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| `cycle_count` | Tag Cycle Count Tab | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| `store_validation` | Store Discrepancy Tab | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| `sale` | Sale Dashboard Tab | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| `void` | Void Dashboard Tab | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| `return` | Return Dashboard Tab | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| `dc_validation` | DC Validation Tab | ✅ | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ |
| `dc_encoding` | DC Encoding Tab | ✅ | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ |
| `tag_management` | Tag Management Tab | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| `vendor_discrepancy` | Vendor Discrepancy Tab | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| `store_counter_status` | Store Counter Status | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| `tag_cleaning` | Tag Cleaning Report | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ |
| `get_sap_stock_take` | SAP Stock Take Report | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ |
| `dispatch_tracking` | Dispatch Tracking | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| `picklist_creation` | Picklist Creation | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| `user_registration` | User Registration | ✅ | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ |
| `store_registration` | Store Registration | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| `warehouse_registration` | Warehouse Registration | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| `floor_registration` | Floor Registration | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |

---

## 👥 Role Descriptions & Scopes

### 1. Super Admin
* **Scope**: Unrestricted platform-wide access across India.
* **Sections**: All 10 Dashboard tabs, Store reports, Tag hygiene reports, and all 4 Master Registrations.
* **Exclusions**: `dispatch_tracking` and `picklist_creation` are kept strictly exclusive to Dispatch Admin.

### 2. Store Admin
* **Scope**: Single assigned store (`Store_ID` / `Store_Code`) or roving store administrator.
* **Sections**: All Store Dashboard tabs (`live_stock`, `cycle_count`, `store_validation`, `sale`, `void`, `return`), Store Counter Status, SAP Stock Take, User Registration (for managing store-level users), and Floor Registration (for configuring store layout/floors).

### 3. Store User / Store
* **Scope**: Single assigned store (`Store_ID` / `Store_Code`).
* **Sections**: All Store Dashboard tabs and Store Counter Status. Read/operate POS floor data.

### 4. Warehouse Admin / WH Admin
* **Scope**: Central Distribution Hub operations.
* **Sections**: All DC tabs (`dc_validation`, `dc_encoding`, `tag_management`, `vendor_discrepancy`), and User Registration.

### 5. Warehouse User / Warehouse
* **Scope**: DC floor operations.
* **Sections**: Scanning and encoding only (`dc_validation`, `dc_encoding`).

### 6. Dispatch Admin
* **Scope**: Dedicated logistics and outward dispatch tracking.
* **Sections**: Exclusively `dispatch_tracking` and `picklist_creation`. Isolated from retail POS and warehouse floors.

### 7. Tag Admin
* **Scope**: RFID tag lifecycle and audit reports.
* **Sections**: Exclusively `tag_cleaning` and `get_sap_stock_take`.
