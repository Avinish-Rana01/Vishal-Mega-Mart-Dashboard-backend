        ### WebForms to .NET Core Migration
- **Legacy SQL Parameters**: When migrating legacy ASP.NET Web Forms ADO.NET code to modern .NET Core, always map missing or null string parameters to `""` (Empty String) instead of `DBNull.Value`. Legacy Stored Procedures in this codebase often expect empty strings, and sending `NULL` will cause `WHERE` clauses to fail and return zero records.

### Project Structure
- **Backend Directory**: Always create new C# backend code (e.g., API Controllers, Models, Services) inside the active `VS Mart Backend` project directory (e.g., `c:\Users\MARKSS\OneDrive\Documents\VS mart Backend\VS Mart Backend\VS Mart Backend\`). Never create project files in temporary or learning directories unless explicitly requested.
- **File Splitting Strategy**: Although splitting large files into smaller components (like DTOs, Services) is the standard, DO NOT automatically split large existing C# files at this time unless explicitly requested. 
