# VS Mart Backend Agent Rules

# Git Operations Constraint
- ONLY execute git commands in the repository corresponding to the active working directory.
- NEVER automatically commit or push code unless explicitly instructed by the user.

# Documentation Synchronization Command (/update-docs or /sync-docs)
When the user types `/update-docs`, `/sync-docs`, or instructs the agent to "update docs with [new tech/feature]":
1. **Scope of Discovery**:
   - Inspect ALL `.md` files in `md/`:
     - `md/system_design/01_CURRENT_SYSTEM_DESIGN.md`
     - `md/system_design/02_PROPOSED_NEXTGEN_SYSTEM_DESIGN.md`
     - `md/system_design/03_COMPARISON_AND_DIFFERENCES.md`
     - `md/FRONTEND_DEVELOPER_BACKEND_GUIDE.md`
     - `md/README.md`
     - `md/WEBSOCKET_DB_TEST_QUERIES.md`
     - `md/ROLES_AND_PERMISSIONS.md`
2. **Synchronization Protocol**:
   - **System Topology & Diagrams**: Update all Mermaid diagrams and component layers to reflect the new technology.
   - **Syntax & Production Code**: Add exact, runnable C# and SQL code snippets showing configuration, dependency injection, and usage.
   - **Evolution & Benchmark Documentation**: Update `03_COMPARISON_AND_DIFFERENCES.md` detailing why the tech was adopted, what problem it solved, and before/after benchmarks.
   - **Cross-Stack Coherence**: If the change modifies API contracts or SignalR channels, coordinate updates with the frontend documentation in `POS_Web_Application React V2/md/`.