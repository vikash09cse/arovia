## **URO CARE HMS** 

Hospital Management System **SaaS Multi-Tenant Edition — Phase 1** 

## **FUNCTIONAL REQUIREMENT DOCUMENT** 

## (FRD) 

Version 2.0 — Phase 1 (Core Operations) Date: July 4, 2026 Initial Customer: Uro Care Hospital Platform: Multi-Tenant SaaS **Classification: Confidential** 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

## **Table of Contents** 

Confidential — Uro Care HMS SaaS  |  Page _2_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

## **1. Document Overview** 

## **1.1 Purpose** 

This Functional Requirement Document (FRD) defines the specifications for Phase 1 of the Uro Care Hospital Management System (HMS), designed as a SaaS multi-tenant platform. While the initial deployment serves Uro Care Hospital as the first customer, the system is architected to onboard multiple hospitals and clinics as independent tenants with complete data isolation and independent configuration. 

Phase 1 focuses on core hospital operations: tenant onboarding, login management, patient management, visit tracking, payment collection, lab tests, and daily reporting. Subscription management, automated billing, plan-based feature gating, and usage-limit enforcement are deferred to Phase 2. 

## **1.2 Scope** 

## **1.2.1 Phase 1 Scope (This Document)** 

- Platform Administration — Manual tenant onboarding and management by Platform Admin [SaaS-Specific] 

- Tenant Configuration — Per-tenant settings (fee amount, free visit window, branding, timezone) [SaaS-Specific] 

- Login and Authentication Management — Tenant-scoped authentication for Tenant Super Admin, Staff, and Doctors 

- Patient Registration and Profile Management — Tenant-isolated patient records 

- Patient Visit Tracking — With configurable fee-based billing logic per tenant 

- Payment Processing and Collection History — With collector tracking 

- Patient Lab Test Management — Simple test ordering and result tracking 

- Daily Reporting Dashboards — For Admin and Doctors 

- Data Isolation and Security — TenantId-based isolation across all modules 

## **1.2.2 Phase 2 Scope (Deferred — Not in This Document)** 

The following features are planned for Phase 2 and are explicitly excluded from the current development scope: 

- Subscription Plan Management (Basic / Professional / Enterprise tiers) 

- Usage-based plan limits (max users, max patients, max storage) 

- Automated billing and invoice generation 

- Trial period management with auto-suspension on expiry 

- Plan upgrade/downgrade workflows 

- Usage tracking dashboards and 80%/90% limit warnings 

- Payment gateway integration for subscription fees 

- API access management and rate limiting per plan tier 

In Phase 1, all tenant onboarding, activation, and suspension is handled manually by the Platform Admin. There are no automated usage limits or plan restrictions. 

Confidential — Uro Care HMS SaaS  |  Page _3_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

## **1.3 SaaS Architecture Overview** 

The system follows a shared-application, shared-database multi-tenant architecture with logical data isolation using a Tenant ID on every data entity. 

|**Aspect**|**Approach**|**Rationale**|
|---|---|---|
|Tenant Isolation|Shared database with TenantId<br>column on every table and EF Core<br>global query filters|Cost-effective for early stage; scales to<br>100+ tenants|
|Tenant<br>Resolution|Subdomain-based (e.g.,<br>urocare.platform.com)|Clean URL separation; adding a new<br>tenant requires zero code changes|
|Authentication|Tenant-scoped JWT tokens with<br>TenantId claim|All API calls are automatically scoped to<br>the correct tenant|
|Data Access|All queries filtered by TenantId<br>automatically via middleware|Prevents cross-tenant data leakage at the<br>infrastructure level|
|File Storage|Tenant-specific folders in cloud<br>storage (e.g., Azure Blob)|Physical separation of uploaded files (lab<br>reports)|
|Configuration|Per-tenant settings table (fee<br>amounts, hospital name, branding,<br>timezone)|Each hospital customizes without affecting<br>others|



## **1.4 User Role Hierarchy** 

|**Level**|**Role**|**Scope**|**Description**|
|---|---|---|---|
|Platform<br>Level|Platform Admin|Entire Platform|Manages all tenants, platform health. Not<br>visible to tenant users.|
|Tenant Level|Tenant Super<br>Admin|Single Tenant|Hospital administrator with full access within<br>their tenant.|
|Tenant Level|Staff|Single Tenant|Front desk / reception / billing personnel<br>within a single hospital.|
|Tenant Level|Doctor|Single Tenant|Consulting physician within a single hospital.|



## **1.5 Intended Audience** 

|**Audience**|**Purpose**|
|---|---|
|Development Team|To build the system per specifications, ensuring multi-tenant isolation|
|QA / Testers|To derive test cases including cross-tenant isolation testing|
|UI/UX Designers|To design screens with tenant branding and role-based layouts|
|Project Manager|To track scope, plan sprints, and manage delivery|
|Hospital Administration|To review and approve tenant-level requirements|



Confidential — Uro Care HMS SaaS  |  Page _4_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

## **1.6 Document Conventions** 

|**Convention**|**Meaning**|
|---|---|
|Must / Shall|Mandatory requirement|
|Should|Recommended but not mandatory|
|May|Optional or future enhancement|
|FR-XX-YYY|Functional Requirement ID (Module-SubModule-Sequence)|
|[SaaS-Specific]|Requirement specific to multi-tenant SaaS architecture|
|[Tenant-Scoped]|Requirement that operates within a single tenant’s data boundary|
|[Phase 2]|Deferred to Phase 2; not in current development scope|



## **1.7 Revision History** 

|**Version**|**Date**|**Author**|**Description**|
|---|---|---|---|
|1.0|July 04, 2026|Uro Care IT Team|Initial FRD (single-tenant)|
|2.0|7/4/2026|Uro Care IT Team|SaaS multi-tenant architecture (Phase 1<br>— subscription deferred to Phase 2)|



Confidential — Uro Care HMS SaaS  |  Page _5_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

## **2. Platform Administration [SaaS-Specific]** 

## **2.1 Module Overview** 

The Platform Administration module is the top-level management layer accessible only to the Platform Admin. In Phase 1, this module handles manual tenant onboarding, activation, suspension, and basic platform monitoring. Tenant-level users (Tenant Super Admin, Staff, Doctors) never see or access this module. 

Note: Subscription plan management, automated billing, usage-limit enforcement, and trial management are deferred to Phase 2. In Phase 1, the Platform Admin manually controls all tenant lifecycle operations. 

## **2.2 Tenant Onboarding** 

## **2.2.1 Functional Requirements** 

|**Req ID**|**Requirement Description**|**Priority**|**Type**|
|---|---|---|---|
|FR-<br>PA-001|The Platform Admin shall be able to create a new tenant<br>by providing: Hospital/Clinic Name, Subdomain (e.g.,<br>urocare), Primary Contact Name, Primary Contact Email,<br>Primary Contact Phone, Address, and Timezone.|High|Functional|
|FR-<br>PA-002|Upon tenant creation, the system shall automatically: (a)<br>create the tenant record with a unique Tenant ID (UUID),<br>(b) provision a subdomain (e.g., urocare.platform.com),<br>(c) create a default Tenant Super Admin account with<br>temporary credentials, (d) send a welcome email with<br>login URL and temporary password, and (e) apply default<br>configuration settings.|High|Functional|
|FR-<br>PA-003|The system shall validate that the requested subdomain is<br>unique, contains only lowercase alphanumeric characters<br>and hyphens, and is not a reserved word (e.g., admin, api,<br>www, app, platform).|High|Validation|
|FR-<br>PA-004|The Platform Admin shall be able to view a Tenant<br>Dashboard showing: Tenant Name, Subdomain, Status<br>(Active/Suspended), Created Date, Total Users, Total<br>Patients, and Last Activity Date.|High|Functional|
|FR-<br>PA-005|The Platform Admin shall be able to suspend a tenant,<br>which immediately prevents all users of that tenant from<br>logging in and displays a 'Your account has been<br>suspended. Please contact support.' message. Active<br>sessions shall be terminated.|High|Functional|
|FR-<br>PA-006|The Platform Admin shall be able to reactivate a<br>suspended tenant, restoring full access for all its users.|Medium|Functional|
|FR-<br>PA-007|The Platform Admin shall be able to edit tenant details<br>(hospital name, contact info, timezone) after creation.|Medium|Functional|
|FR-<br>PA-008|The Platform Admin shall be able to view basic usage<br>analytics per tenant: number of active users, total<br>patients, total visits (monthly), and total lab tests|Medium|Reporting|



Confidential — Uro Care HMS SaaS  |  Page _6_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

(monthly). 

## **2.2.2 Tenant Record Data Fields** 

|**Field Name**|**Data Type**|**Required**|**Details**|
|---|---|---|---|
|Tenant ID|UUID (Auto)|System|Globally unique identifier|
|Hospital/Clinic Name|String (200)|Yes|Display name of the tenant|
|Subdomain|String (50)|Yes|Unique; lowercase alphanumeric and<br>hyphens only|
|Primary Contact Name|String (100)|Yes|Main point of contact|
|Primary Contact Email|String (100)|Yes|Used for Super Admin account creation|
|Primary Contact Phone|String (15)|Yes|Support contact number|
|Address|Text (500)|Yes|Hospital/clinic physical address|
|Status|Enum|System|Active / Suspended (manually managed in<br>Phase 1)|
|Created Date|DateTime|System|Auto-populated|
|Logo URL|String (500)|No|Tenant’s logo for branding|
|Timezone|String (50)|Yes|e.g., Asia/Kolkata; used for all date/time<br>display|



## **2.3 Platform Monitoring Dashboard (Phase 1)** 

|**Req ID**|**Requirement Description**|**Priority**|**Type**|
|---|---|---|---|
|FR-<br>PA-009|The Platform Admin dashboard shall display: Total<br>Tenants, Active Tenants, Suspended Tenants, Total<br>Users across all tenants, and Total Patients across all<br>tenants.|High|Reporting|
|FR-<br>PA-010|The Platform Admin shall be able to view system health:<br>API response times and error rates.|Medium|Reporting|
|FR-<br>PA-011|The Platform Admin shall be able to impersonate a<br>Tenant Super Admin for troubleshooting, with all actions<br>logged in the audit trail.|Low|Functional|



## **2.4 Business Rules** 

1. Only the Platform Admin can create, suspend, and reactivate tenants. 

2. Tenant status in Phase 1 is limited to Active and Suspended (no Trial or Cancelled). 

3. Suspended tenants retain all data; no data is deleted on suspension. 

4. Platform Admin accesses the system via a dedicated URL (e.g., admin.platform.com), completely separate from tenant subdomains. 

5. In Phase 1, there are no automated limits on users, patients, or storage per tenant. All tenants have unrestricted access to all features. 

Confidential — Uro Care HMS SaaS  |  Page _7_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

## **2.5 Acceptance Criteria** 

6. Platform Admin can create a new tenant, and the subdomain becomes accessible with a branded login page within 5 minutes. 

7. A welcome email with temporary credentials is sent to the primary contact upon tenant creation. 

8. Suspending a tenant immediately blocks all login attempts for that tenant. 

9. Reactivating a tenant restores access with all data intact. 

10. Platform dashboard shows accurate counts across all tenants. 

Confidential — Uro Care HMS SaaS  |  Page _8_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

## **3. Tenant Configuration [SaaS-Specific]** 

## **3.1 Module Overview** 

Each tenant (hospital/clinic) has its own configuration settings managed by the Tenant Super Admin. Configuration changes for one tenant have zero impact on other tenants. This module allows each hospital to customize their consultation fee, billing rules, branding, and display preferences. 

## **3.2 Functional Requirements** 

|**Req ID**|**Requirement Description**|**Priority**|**Type**|
|---|---|---|---|
|FR-<br>TC-001|Each tenant shall have an independent Settings page<br>accessible only to the Tenant Super Admin, containing:<br>Hospital Name, Address, Contact Details, Logo,<br>Timezone, and Currency.|High|Functional|
|FR-<br>TC-002|The Tenant Super Admin shall be able to configure the<br>Consultation Fee Amount, which is used by the visit billing<br>logic for that tenant.|High|Functional|
|FR-<br>TC-003|The Tenant Super Admin shall be able to configure the<br>Free Visit Window (default: 10 days), allowing each<br>hospital to set their own follow-up billing interval.|High|Functional|
|FR-<br>TC-004|The Tenant Super Admin shall be able to manage their<br>own Lab Test Master List independently of other tenants.|High|Functional|
|FR-<br>TC-005|The Tenant Super Admin shall be able to upload and<br>manage the hospital logo, which shall appear on printed<br>receipts, reports, and the tenant’s login page.|Medium|Functional|
|FR-<br>TC-006|The Tenant Super Admin shall be able to configure<br>receipt header/footer text (e.g., GST number, registration<br>number, custom message).|Medium|Functional|
|FR-<br>TC-007|The Tenant Super Admin shall be able to configure the<br>Patient ID prefix (e.g., UC- for Uro Care).|Medium|Functional|
|FR-<br>TC-008|All configuration changes shall be logged in the audit trail<br>with user, timestamp, old value, and new value.|Medium|Audit|



## **3.3 Tenant Configuration Data Fields** 

|**Field Name**|**Data Type**|**Default**|**Details**|
|---|---|---|---|
|Hospital Name|String (200)|From onboarding|Displayed in header, receipts,<br>reports|
|Hospital Address|Text (500)|From onboarding|Displayed on receipts|
|Hospital Phone|String (15)|From onboarding|Contact number|
|Hospital Email|String (100)|From onboarding|Contact email|
|Logo|Image URL|Platform default|Max 2 MB; PNG/JPG|



Confidential — Uro Care HMS SaaS  |  Page _9_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

|Timezone|String (50)|Asia/Kolkata|Used for all date/time display|
|---|---|---|---|
|Currency|String (3)|INR|Currency symbol for display|
|Consultation Fee Amount|Decimal|0.00|Charged per billable visit|
|Free Visit Window (Days)|Integer|10|Days after charged visit during<br>which follow-ups are free|
|Patient ID Prefix|String (10)|From hospital<br>name|e.g., UC- for Uro Care|
|Receipt Header Text|Text (300)|Blank|Custom text on receipt header|
|Receipt Footer Text|Text (300)|Blank|Custom text on receipt footer|
|GST / Tax Number|String (50)|Blank|Displayed on receipts if provided|



## **3.4 Acceptance Criteria** 

11. Tenant Super Admin can update all configuration fields and see changes reflected immediately. 

12. Changing the consultation fee applies to new visits only; existing visit records are not retroactively modified. 

13. Changing the Free Visit Window applies to new visit calculations only. 

14. Configuration changes in Tenant A have zero effect on Tenant B. 

15. Audit trail captures all configuration changes with before/after values. 

Confidential — Uro Care HMS SaaS  |  Page _10_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

## **4. Login Management [Tenant-Scoped]** 

## **4.1 Module Overview** 

The Login Management module provides secure, tenant-scoped authentication and session management. When a user accesses the system via a tenant’s subdomain (e.g., urocare.platform.com), the system resolves the tenant and authenticates the user within that tenant’s boundary. Users of one tenant cannot access another tenant’s data or even see that other tenants exist. 

## **4.2 Functional Requirements** 

|**Req ID**|**Requirement Description**|**Priority**|**Type**|
|---|---|---|---|
|FR-<br>LM-001|The system shall resolve the tenant from the subdomain<br>in the URL before displaying the login page. If the<br>subdomain does not match any active tenant, a 'Hospital<br>not found' page shall be displayed.|High|Functional|
|FR-<br>LM-002|The login page shall display the tenant’s hospital name<br>and logo (if configured), providing a branded experience.|Medium|Functional|
|FR-<br>LM-003|The system shall authenticate users against credentials<br>stored within the resolved tenant’s scope. A user’s email<br>can exist in multiple tenants as separate accounts.|High|Security|
|FR-<br>LM-004|Upon successful login, the system shall issue a JWT<br>token containing: User ID, Tenant ID, Role, and token<br>expiry. All subsequent API requests shall validate the<br>Tenant ID in the token.|High|Security|
|FR-<br>LM-005|The system shall display appropriate error messages for<br>invalid credentials without revealing whether the<br>username or password is incorrect.|High|Security|
|FR-<br>LM-006|The system shall lock a user account after 5 consecutive<br>failed login attempts for 15 minutes.|Medium|Security|
|FR-<br>LM-007|The system shall provide a 'Forgot Password' feature that<br>sends a tenant-context-aware password reset link.|Medium|Functional|
|FR-<br>LM-008|Password reset links shall expire after 30 minutes.|Medium|Security|
|FR-<br>LM-009|The system shall enforce password complexity: minimum<br>8 characters with at least one uppercase, one lowercase,<br>one digit, and one special character.|High|Security|
|FR-<br>LM-010|The system shall maintain user sessions with configurable<br>timeout (default: 30 minutes of inactivity).|High|Functional|
|FR-<br>LM-011|The system shall provide a Logout button on every page,<br>terminating the session and redirecting to the tenant’s<br>login page.|High|Functional|
|FR-<br>LM-012|Tenant Super Admin shall be able to create, edit, activate,<br>and deactivate user accounts for Staff and Doctors within<br>their tenant only.|High|Functional|



Confidential — Uro Care HMS SaaS  |  Page _11_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

|FR-<br>LM-013|The system shall log all login attempts (successful and<br>failed) with timestamp, IP address, user identifier, and<br>Tenant ID.|Medium|Audit|
|---|---|---|---|
|FR-<br>LM-014|If a tenant is suspended, all login attempts shall be<br>rejected with: 'Your organization’s account is currently<br>inactive. Please contact support.'|High|Business<br>Logic|



## **4.3 Business Rules** 

16. Tenant resolution from subdomain is the first step before any authentication logic executes. 

17. User accounts are tenant-scoped: the same email can exist in multiple tenants with different roles and passwords. 

18. Only Tenant Super Admin can manage user accounts within their tenant. 

19. Platform Admin accesses the system via a separate URL (admin.platform.com). 

20. All passwords must be stored using one-way hashing (bcrypt or equivalent). 

21. JWT tokens must include TenantId as a claim; API middleware validates on every request. 

## **4.4 UI / Screen Descriptions** 

## **4.4.1 Tenant Login Screen** 

- Hospital logo and name (from tenant configuration) displayed prominently 

- Username / Email input field 

- Password input field with show/hide toggle 

- 'Login' button (primary action) 

- 'Forgot Password?' link 

- Tenant subdomain visible in the browser URL bar 

## **4.4.2 User Management Screen (Tenant Super Admin Only)** 

- Searchable and filterable list of all users within this tenant 

- Columns: Name, Email, Role, Status (Active/Inactive), Last Login, Actions 

- 'Add New User' button: Full Name, Email, Role (dropdown), Temporary Password 

- Edit and Deactivate/Activate action buttons per row 

## **4.5 Acceptance Criteria** 

22. Accessing urocare.platform.com shows Uro Care Hospital’s branded login page. 23. An invalid subdomain shows 'Hospital not found' page. 

24. A user of Tenant A cannot authenticate against Tenant B. 

25. JWT token contains the correct TenantId; all API calls are scoped. 

26. Login attempt for a suspended tenant is rejected with the inactive message. 

Confidential — Uro Care HMS SaaS  |  Page _12_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

## **5. Patient Management [Tenant-Scoped]** 

## **5.1 Module Overview** 

The Patient Management module is the central hub of the HMS. All patient data is strictly isolated by tenant. A patient registered in Uro Care Hospital is not visible to any other hospital. Patient IDs are unique within a tenant (the combination of TenantId + PatientId is the global unique key). The module covers patient registration, multi-visit tracking, payment history, lab tests, and daily reporting. 

## **5.2 Multi-Tenancy Impact** 

|**Aspect**|**Single-Tenant (v1.0)**|**SaaS Multi-Tenant (v2.0)**|
|---|---|---|
|Patient ID|Unique in system (UC-<br>XXXXX)|Unique within tenant; each tenant has its own<br>sequence|
|Patient ID Prefix|Fixed: UC-|Configurable per tenant via Tenant<br>Configuration|
|Phone Uniqueness|Unique across entire system|Unique within tenant; same phone can exist in<br>different tenants|
|Consultation Fee|Fixed system-wide|Configurable per tenant|
|Free Visit Window|Fixed at 10 days|Configurable per tenant (default 10 days)|
|Lab Test Master List|Single shared list|Independent master list per tenant|
|Data Visibility|All users see all patients|Users see only patients belonging to their<br>tenant|
|Reports|Single dataset|Scoped to tenant; no cross-tenant aggregation|



Confidential — Uro Care HMS SaaS  |  Page _13_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

## **5.3 Patient Registration** 

## **5.3.1 Overview** 

Patient Registration captures demographic, contact, and medical reference information within a tenant. Each patient receives a unique Patient ID with the tenant’s configured prefix upon registration. 

## **5.3.2 Functional Requirements** 

|**Req ID**|**Requirement Description**|**Priority**|**Type**|
|---|---|---|---|
|FR-<br>PR-001|The system shall provide a Patient Registration form<br>capturing: Full Name, Date of Birth, Gender, Phone<br>Number, Email (optional), Address, Emergency Contact<br>Name and Phone, Blood Group, and Referred By.|High|Functional|
|FR-<br>PR-002|The system shall auto-generate a unique Patient ID using<br>the tenant’s configured prefix and a sequential number<br>(e.g., UC-00001). The sequence is independent per<br>tenant.|High|Functional|
|FR-<br>PR-003|Phone Number shall be unique within the current tenant.<br>The same phone number in a different tenant is allowed.|High|Validation|
|FR-<br>PR-004|The system shall display a confirmation with the<br>generated Patient ID after successful registration.|Medium|Functional|
|FR-<br>PR-005|Patient Search (by Patient ID, Name, or Phone) shall only<br>return patients belonging to the current tenant.|High|Functional|
|FR-<br>PR-006|Staff and Tenant Super Admin can edit patient details<br>after registration.|High|Functional|
|FR-<br>PR-007|Patient list view: Patient ID, Name, Phone, Gender, Last<br>Visit Date, Status.|High|Functional|
|FR-<br>PR-008|The patient list shall support filtering by date range,<br>gender, and status (Active/Inactive).|Medium|Functional|
|FR-<br>PR-009|Doctors shall have view-only access to patient registration<br>details.|High|Functional|
|FR-<br>PR-010|The system shall track registration date and the user who<br>registered the patient.|Medium|Audit|



## **5.3.3 Patient Data Fields** 

|**Field Name**|**Data Type**|**Required**|**Validation**|
|---|---|---|---|
|Tenant ID|UUID (System)|System|Auto-populated from user context|
|Patient ID|Auto-generated|System|Tenant prefix + sequence number|
|Full Name|String (100)|Yes|Alphabets and spaces; min 2 chars|
|Date of Birth|Date|Yes|Cannot be a future date|
|Gender|Enum|Yes|Male / Female / Other|
|Phone Number|String (15)|Yes|Numeric; unique within tenant; 10-15 digits|



Confidential — Uro Care HMS SaaS  |  Page _14_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

|Email|String (100)|No|Valid email format if provided|
|---|---|---|---|
|Address|Text (500)|Yes|Min 10 characters|
|Emergency Contact<br>Name|String (100)|Yes|Alphabets and spaces|
|Emergency Contact<br>Phone|String (15)|Yes|Numeric; 10-15 digits|
|Blood Group|Enum|No|A+, A-, B+, B-, AB+, AB-, O+, O-|
|Referred By|String (100)|No|Doctor name or 'Self'|



## **5.3.4 Business Rules** 

27. All patient data is automatically tagged with the current TenantId at the database level. 

28. A patient must be registered before any visit, payment, or lab test can be recorded. 

29. Patient ID prefix is configurable per tenant; once a patient is registered, their ID does not change even if the prefix is later updated. 

30. Duplicate phone numbers within the same tenant trigger a warning with existing patient details. 

31. Patient records cannot be permanently deleted; only marked as Inactive. 

## **5.3.5 Acceptance Criteria** 

32. A patient registered in Tenant A is not visible in Tenant B. 

33. Patient ID sequence is independent per tenant. 

34. Phone uniqueness is enforced within a tenant but not across tenants. 

35. Patient search returns only patients from the current tenant. 

Confidential — Uro Care HMS SaaS  |  Page _15_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

## **5.4 Patient Visits [Tenant-Scoped]** 

## **5.4.1 Overview** 

The Patient Visits sub-module tracks every visit a patient makes. The system enforces a configurable fee-charging rule: the consultation fee (amount set per tenant) is charged for the first visit, and subsequent visits within the tenant’s configured Free Visit Window (default: 10 days) of the last charged visit are free. After the window expires, the fee is charged again. 

## **5.4.2 Fee Charging Logic (Configurable per Tenant)** 

|**Scenario**|**Fee Charged?**|**Explanation**|
|---|---|---|
|First visit ever|Yes|Initial consultation; fee always charged at tenant’s<br>configured rate|
|Visit within Free Visit Window<br>of last charged visit|No (Free)|Follow-up within the tenant’s complimentary<br>window|
|Visit after Free Visit Window<br>expires|Yes|New billing cycle; fee charged at tenant’s<br>configured rate|
|Visit on exactly the last day of<br>the window|No (Free)|The window is inclusive|
|Visit the day after the window<br>expires|Yes|Complimentary window has expired|



Note: Both the fee amount and free visit window duration are configurable per tenant via the Tenant Configuration module (Section 3). 

## **5.4.3 Functional Requirements** 

|**Req ID**|**Requirement Description**|**Priority**|**Type**|
|---|---|---|---|
|FR-<br>PV-001|Staff and Tenant Super Admin can create a new visit for a<br>registered patient within the current tenant.|High|Functional|
|FR-<br>PV-002|Each visit shall capture: Visit Date (auto-populated),<br>Consulting Doctor (from tenant’s active Doctors),<br>Purpose/Reason, Visit Notes (optional), and Fee Status<br>(Charged/Free).|High|Functional|
|FR-<br>PV-003|The system shall auto-determine fee applicability using<br>the tenant’s configured Free Visit Window. If the gap from<br>the last charged visit exceeds the window, fee is Charged.|High|Business<br>Logic|
|FR-<br>PV-004|Before confirming, the system shall display: fee status<br>(Charged/Free), fee amount (if Charged), and date of last<br>charged visit.|High|Functional|
|FR-<br>PV-005|Visit history shall display in reverse chronological order:<br>Visit Date, Doctor, Purpose, Fee Status, Payment Status.|High|Functional|
|FR-<br>PV-006|Each visit shall have a unique Visit ID (e.g., VIS-<br>XXXXXXX) within the tenant.|High|Functional|
|FR-<br>PV-007|Doctors shall have view-only access to visit records within<br>their tenant.|High|Functional|



Confidential — Uro Care HMS SaaS  |  Page _16_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

|FR-<br>PV-008|Visit notes can be updated after creation, but Visit Date<br>and Fee Status cannot be edited.|Medium|Functional|
|---|---|---|---|
|FR-<br>PV-009|Tenant Super Admin can override fee status with a<br>mandatory reason.|Medium|Functional|
|FR-<br>PV-010|Patient profile shall show: total visits, total charged, total<br>free.|Medium|Functional|



## **5.4.4 Visit Record Data Fields** 

|**Field Name**|**Data Type**|**Required**|**Details**|
|---|---|---|---|
|Tenant ID|UUID (System)|System|Auto-populated; ensures tenant<br>isolation|
|Visit ID|Auto-generated|System|VIS-XXXXXXX; unique within tenant|
|Patient ID|Reference|Yes|Links to patient within tenant|
|Visit Date & Time|DateTime|Yes|Auto-populated in tenant’s timezone|
|Consulting Doctor|Dropdown|Yes|Active Doctors within this tenant|
|Purpose / Reason|Text (300)|Yes|Reason for the visit|
|Visit Notes|Text (1000)|No|Doctor or Staff notes|
|Fee Status|Enum|System|Charged / Free (auto-calculated using<br>tenant’s window)|
|Fee Amount|Decimal|Conditiona<br>l|From tenant config; required if Charged|
|Days Since Last Charged<br>Visit|Integer|System|Calculated and displayed|
|Created By|Reference|System|User who created the visit|



## **5.4.5 Business Rules** 

36. Free Visit Window duration is read from the tenant’s configuration at visit creation time. 37. Fee amount is the tenant’s configured Consultation Fee Amount at the time of the visit. 38. The window is calculated from the last 'Charged' visit, not from the last visit of any type. 39. Visit Date defaults to current date/time in the tenant’s timezone. 

40. Visits cannot be deleted; only Tenant Super Admin can mark as 'Cancelled' with a reason. 

## **5.4.6 Acceptance Criteria** 

41. Fee logic uses the tenant’s configured window correctly (e.g., Tenant A = 10 days, Tenant B = 7 days). 

42. Fee amount reflects the tenant’s configured consultation fee. 

43. Visits in Tenant A are not visible in Tenant B. 

44. Doctor dropdown shows only doctors from the current tenant. 

Confidential — Uro Care HMS SaaS  |  Page _17_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

## **5.5 Payment History [Tenant-Scoped]** 

## **5.5.1 Overview** 

Records all payment transactions for patient visits within a tenant. Every charged visit generates a payment record. Tracks collector identity, timing, method, and amount with full audit trail. 

## **5.5.2 Functional Requirements** 

|**Req ID**|**Requirement Description**|**Priority**|**Type**|
|---|---|---|---|
|FR-<br>PH-001|A payment record in 'Pending' status is auto-created when<br>a visit is marked as 'Charged'.|High|Functional|
|FR-<br>PH-002|Staff and Tenant Super Admin can collect payment,<br>recording: Collected By, Collection Date/Time (tenant<br>timezone), Payment Method, and Amount Paid.|High|Functional|
|FR-<br>PH-003|Payment methods: Cash, Card, UPI, Online Transfer.|High|Functional|
|FR-<br>PH-004|Patient payment history shows: Visit Date, Visit ID, Fee<br>Amount, Payment Status, Method, Collected By,<br>Collection Date.|High|Functional|
|FR-<br>PH-005|Global payment view (Tenant Super Admin/Staff) shows<br>all payments within the tenant with filters for Date Range,<br>Status, Method, and Collected By.|High|Functional|
|FR-<br>PH-006|Each payment receives a unique Receipt Number (RCT-<br>XXXXXXX) within the tenant.|Medium|Functional|
|FR-<br>PH-007|Printable receipt uses the tenant’s branding: logo, hospital<br>name, address, GST number, header/footer text.|Medium|Functional|
|FR-<br>PH-008|Tenant Super Admin can view total payments collected by<br>each Staff member for a date range.|Medium|Reporting|
|FR-<br>PH-009|Doctors have view-only access to payment history for<br>their patients.|Medium|Functional|
|FR-<br>PH-010|Completed payments cannot be modified. Only Tenant<br>Super Admin can mark as 'Refunded' with a mandatory<br>reason.|High|Business<br>Logic|



## **5.5.3 Payment Record Data Fields** 

|**Field Name**|**Data Type**|**Required**|**Details**|
|---|---|---|---|
|Tenant ID|UUID (System)|System|Auto-populated|
|Receipt Number|Auto-generated|System|RCT-XXXXXXX; unique within tenant|
|Patient ID|Reference|Yes|Within tenant|
|Visit ID|Reference|Yes|Links to the visit|
|Fee Amount|Decimal|Yes|From tenant config|
|Amount Paid|Decimal|Yes|Amount collected|
|Payment Status|Enum|System|Pending / Paid / Refunded|



Confidential — Uro Care HMS SaaS  |  Page _18_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

|Payment Method|Enum|Conditiona<br>l|Cash / Card / UPI / Online Transfer|
|---|---|---|---|
|Collected By|Reference|System|Logged-in user|
|Collection Date/Time|DateTime|System|In tenant’s timezone|
|Refund Reason|Text (300)|Conditiona<br>l|Required if Refunded|



## **5.5.4 Acceptance Criteria** 

45. Payment records in Tenant A are not visible in Tenant B. 

46. Printed receipts display the correct tenant’s branding and GST details. 

47. Collector identity is accurately recorded from the tenant’s user context. 

48. Global payment view is scoped to the current tenant only. 

Confidential — Uro Care HMS SaaS  |  Page _19_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

## **5.6 Patient Lab Tests [Tenant-Scoped]** 

## **5.6.1 Overview** 

Each tenant maintains its own independent lab test master list. Lab test data (orders, results, uploaded reports) is strictly tenant-isolated. Uploaded report files are stored in tenant-specific storage paths. 

## **5.6.2 Functional Requirements** 

|**Req ID**|**Requirement Description**|**Priority**|**Type**|
|---|---|---|---|
|FR-<br>LT-001|Each tenant has its own lab test master list, managed by<br>Tenant Super Admin. Default urology tests are seeded<br>during onboarding.|High|Functional|
|FR-<br>LT-002|Doctors and Staff can order lab tests for a patient, linked<br>to a visit within the tenant.|High|Functional|
|FR-<br>LT-003|Each order captures: Patient ID, Visit ID, Test Name (from<br>tenant’s master list), Ordered By, Order Date/Time (tenant<br>timezone), Priority (Routine/Urgent).|High|Functional|
|FR-<br>LT-004|Status workflow: Ordered → Sample Collected → In<br>Progress → Completed / Cancelled.|High|Functional|
|FR-<br>LT-005|Staff can update the status as the test progresses.|High|Functional|
|FR-<br>LT-006|On completion, Staff enters findings/remarks and<br>optionally uploads a report file (PDF/image). Files stored<br>in tenant’s dedicated storage folder.|Medium|Functional|
|FR-<br>LT-007|Doctors can view lab test orders and results for patients in<br>their tenant.|High|Functional|
|FR-<br>LT-008|Patient lab test history shows all past tests, results, and<br>statuses.|High|Functional|
|FR-<br>LT-009|Each test order has a unique Lab Test ID (LAB-<br>XXXXXXX) within the tenant.|Medium|Functional|
|FR-<br>LT-010|Tenant Super Admin can add, edit, or deactivate tests in<br>the master list.|Medium|Functional|



## **5.6.3 Acceptance Criteria** 

49. Tenant A’s lab test master list is independent of Tenant B’s. 50. Lab test orders/results in Tenant A are not visible in Tenant B. 51. Uploaded files are stored in tenant-specific storage paths. 52. Lab test status workflow progresses correctly. 

Confidential — Uro Care HMS SaaS  |  Page _20_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

## **5.7 Daily Reporting Dashboard [Tenant-Scoped]** 

## **5.7.1 Overview** 

Provides tenant-level visibility into today’s hospital activity. All data is scoped to the current tenant, with 'today' defined using the tenant’s configured timezone. 

## **5.7.2 Functional Requirements** 

|**Req ID**|**Requirement Description**|**Priority**|**Type**|
|---|---|---|---|
|FR-<br>DR-001|Today’s Visits report shows all patients who visited today<br>within the current tenant, using the tenant’s timezone.|High|Reporting|
|FR-<br>DR-002|Columns: Patient ID, Patient Name, Visit Time (tenant<br>timezone), Consulting Doctor, Purpose, Fee Status,<br>Payment Status.|High|Reporting|
|FR-<br>DR-003|Doctors see only their visits; Tenant Super Admin and<br>Staff see all visits within the tenant.|High|Functional|
|FR-<br>DR-004|Summary cards: Total Visits Today, Charged Visits, Free<br>Visits, Payments Collected Today (tenant’s currency),<br>Payments Pending.|High|Reporting|
|FR-<br>DR-005|Lab Test Summary: Total Ordered Today, Completed, In<br>Progress, Pending, Urgent — all within the tenant.|High|Reporting|
|FR-<br>DR-006|Lab Test Summary is clickable to drill down into individual<br>tests.|Medium|Functional|
|FR-<br>DR-007|Dashboard auto-refreshes every 5 minutes or has a<br>manual Refresh button.|Medium|Functional|
|FR-<br>DR-008|Tenant Super Admin can filter by Doctor, Fee Status, and<br>Payment Status.|Medium|Functional|
|FR-<br>DR-009|Export to PDF/Excel includes the tenant’s branding in the<br>header.|Low|Functional|
|FR-<br>DR-010|7-day patient visit trend chart on the Tenant Super Admin<br>dashboard.|Low|Reporting|



## **5.7.3 Acceptance Criteria** 

53. Dashboard data is scoped to the current tenant. 

54. 'Today' uses the tenant’s configured timezone. 

55. Doctor sees only their patients; Admin sees all. 

56. Exported reports carry the tenant’s branding. 

Confidential — Uro Care HMS SaaS  |  Page _21_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

## **6. Data Isolation and Security [SaaS-Specific]** 

## **6.1 Overview** 

Data isolation is the most critical aspect of the SaaS architecture. Every piece of data belongs to exactly one tenant. The system guarantees that no tenant can ever access another tenant’s data through any mechanism — UI, API, reports, search, or file access. 

## **6.2 Functional Requirements** 

|**Req ID**|**Requirement Description**|**Priority**|**Type**|
|---|---|---|---|
|FR-<br>DI-001|Every tenant-specific database table shall include a<br>TenantId column as a mandatory foreign key.|High|Security|
|FR-<br>DI-002|The application shall apply automatic global query filters<br>(EF Core HasQueryFilter) on all tenant-scoped entities,<br>ensuring every query includes WHERE TenantId =<br>@currentTenantId.|High|Security|
|FR-<br>DI-003|All INSERT operations shall automatically set TenantId<br>from the authenticated user’s context. Manual TenantId<br>setting by API consumers shall not be permitted.|High|Security|
|FR-<br>DI-004|API endpoints shall validate that the TenantId in the JWT<br>token matches the requested resource’s TenantId.<br>Mismatch returns HTTP 403.|High|Security|
|FR-<br>DI-005|File uploads shall be stored in tenant-specific paths<br>(e.g., /tenants/{tenantId}/lab-reports/). Access validated<br>against the user’s TenantId.|High|Security|
|FR-<br>DI-006|All search and autocomplete features shall be tenant-<br>scoped. No cross-tenant results.|High|Security|
|FR-<br>DI-007|Data exports shall be tenant-scoped. A Tenant Super<br>Admin export contains only their tenant’s data.|High|Security|
|FR-<br>DI-008|Cross-tenant penetration testing shall be conducted<br>before each major release.|Medium|Security|



## **6.3 Security Requirements** 

|**Requirement**|**Description**|
|---|---|
|Encryption in Transit|TLS 1.2 or higher for all client-server communication|
|Encryption at Rest|AES-256 encryption for database and file storage|
|JWT Security|Tokens include TenantId, UserId, Role; validated on every API request|
|CORS Policy|API accepts requests only from registered tenant subdomains and admin<br>domain|
|Input Validation|Server-side validation to prevent SQL injection, XSS, and other attacks|
|Audit Logging|All CUD operations logged with TenantId, UserId, timestamp, and change<br>details|



Confidential — Uro Care HMS SaaS  |  Page _22_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

GDPR / Data Privacy Support tenant data export (portability) and tenant data deletion (right to be forgotten) 

Confidential — Uro Care HMS SaaS  |  Page _23_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

## **7. Non-Functional Requirements** 

|**Category**|**Requirement**|
|---|---|
|Performance|Pages load within 3 seconds. Tenant resolution from subdomain completes<br>within 100ms.|
|Scalability|Support up to 100 tenants and 500 concurrent users per tenant in Phase 1.|
|Multi-Tenancy|Adding a new tenant requires zero code changes — only a database record and<br>DNS entry.|
|Availability|99.5% uptime. Individual tenant suspension does not affect other tenants.|
|Data Backup|Daily automated backups with 30-day retention. Tenant-specific restore<br>capability.|
|Audit Trail|All operations logged with TenantId, UserId, timestamp, and change details.|
|Browser Support|Latest Chrome, Firefox, Edge, and Safari.|
|Responsive Design|Functional on tablets (minimum 768px width).|
|Timezone Handling|All dates stored in UTC; displayed in tenant’s configured timezone.|
|Localization|English primary; Hindi support planned. Currency per tenant configuration.|
|Compliance|Applicable healthcare data privacy regulations.|



Confidential — Uro Care HMS SaaS  |  Page _24_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

## **8. Technology Stack** 

|**Layer**|**Technology**|**Purpose**|
|---|---|---|
|Backend API|ASP.NET Core 8 (Web API)|RESTful API with multi-tenant middleware,<br>JWT auth, EF Core|
|Frontend|React 18 with TypeScript|SPA with component libraries, TanStack<br>Query for data fetching|
|Database|SQL Server (Azure SQL)|Shared database with TenantId-based<br>isolation|
|Authentication|ASP.NET Identity + JWT|Tenant-scoped user management with role-<br>based authorization|
|File Storage|Azure Blob Storage|Tenant-specific containers for lab report<br>uploads|
|Hosting|Azure App Service|Scalable cloud hosting|
|CI/CD|GitHub Actions / Azure DevOps|Automated build, test, deployment pipeline|
|Monitoring|Application Insights / Serilog|Structured logging with TenantId context|
|Email|SMTP (SendGrid / Azure)|Password reset, welcome emails, notifications|
|DNS|Azure DNS / Cloudflare|Wildcard subdomain routing for tenant<br>resolution|



Confidential — Uro Care HMS SaaS  |  Page _25_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

## **9. Assumptions and Dependencies** 

## **9.1 Assumptions** 

57. Uro Care Hospital is the first tenant, onboarded during initial deployment. 

58. In Phase 1, there are no usage limits or subscription tiers. All tenants have full feature access. 

59. Tenant onboarding, activation, and suspension are manual operations by the Platform Admin. 

60. Subscription plan management, automated billing, and usage-limit enforcement will be added in Phase 2. 

61. Each tenant has stable internet connectivity for their users. 

62. Consultation fee and free visit window are configurable per tenant. 

63. Initial onboarding includes seeding default lab tests relevant to urology practice. 

64. Platform Admin and Tenant Admin are different roles with different access levels and login URLs. 

## **9.2 Dependencies** 

65. Cloud hosting infrastructure (Azure) provisioned before deployment. 

66. Wildcard SSL certificate for *.platform.com subdomain routing. 

67. DNS configuration supporting wildcard subdomains. 

68. Email service (SMTP) for transactional emails across all tenants. 

69. Hospital branding assets (logo) from each tenant during onboarding. 

Confidential — Uro Care HMS SaaS  |  Page _26_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

## **10. Phase 2 Roadmap (Reference Only)** 

The following features are planned for Phase 2 development. They are listed here for architectural awareness so that Phase 1 implementation does not create blockers for these features. No Phase 2 features shall be implemented in Phase 1. 

|**Feature**|**Description**|**Impact on Phase 1 Architecture**|
|---|---|---|
|Subscription Plans|Basic / Professional / Enterprise tiers<br>with different feature sets and limits|Phase 1 should include a<br>SubscriptionPlanId column on the<br>Tenants table (nullable, unused in<br>Phase 1) to avoid schema migration<br>later|
|Usage Limits|Max users, max patients, max storage<br>per plan|Phase 1 data model should track<br>counts (total users, total patients,<br>storage used) even though limits are<br>not enforced|
|Automated Billing|Monthly invoice generation and<br>payment gateway integration|Phase 1 should include a BillingEmail<br>field on the Tenants table|
|Trial Management|14-day trial with auto-suspension on<br>expiry|Phase 1 Tenant status enum should<br>include Trial even if unused, to avoid<br>enum migration later|
|Plan Feature Gating|Certain features restricted by plan (e.g.,<br>receipt printing, export)|Phase 1 should implement features<br>using feature flags (all enabled) so<br>Phase 2 can gate them by plan|
|API Access|External API access for Enterprise plan<br>tenants|Phase 1 API authentication should be<br>designed with API key support in mind|



Confidential — Uro Care HMS SaaS  |  Page _27_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

## **11. Glossary** 

|**Term**|**Definition**|
|---|---|
|HMS|Hospital Management System|
|FRD|Functional Requirement Document|
|SaaS|Software as a Service — cloud-hosted, multi-tenant software delivery<br>model|
|Tenant|A single hospital/clinic on the platform with completely isolated data|
|Tenant ID|UUID identifying each tenant; present on every data record|
|Multi-Tenancy|Architecture allowing multiple customers to share a single application<br>instance|
|Platform Admin|Top-level admin managing all tenants and platform health|
|Tenant Super Admin|Hospital-level admin managing users, settings, and operations within a<br>tenant|
|Patient ID|Unique per patient within a tenant (e.g., UC-00001)|
|Visit ID|Unique per visit within a tenant (e.g., VIS-XXXXXXX)|
|Receipt Number|Unique per payment within a tenant (e.g., RCT-XXXXXXX)|
|Lab Test ID|Unique per lab test order within a tenant (e.g., LAB-XXXXXXX)|
|Free Visit Window|Configurable days after a charged visit during which follow-ups are free|
|Charged Visit|A visit where the consultation fee is applicable|
|Free Visit|A visit within the Free Visit Window; no fee applied|
|JWT|JSON Web Token for stateless authentication; contains TenantId, UserId,<br>Role|
|Global Query Filter|EF Core feature auto-appending WHERE TenantId = X to every query|
|Subdomain Routing|Each tenant accessed via unique subdomain (e.g., urocare.platform.com)|
|UPI|Unified Payments Interface (Indian digital payment)|
|[Phase 2]|Feature deferred to Phase 2 development cycle|



## **12. Approval and Sign-Off** 

This document has been reviewed and approved by the following stakeholders: 

|**Role**|**Name**|**Signature**|**Date**|
|---|---|---|---|
|Hospital Management||||
|Project Manager||||
|Technical Lead||||
|QA Lead||||



Confidential — Uro Care HMS SaaS  |  Page _28_ 

_Uro Care HMS | FRD v2.0 | SaaS Phase 1_ 

## Platform Architect 

_--- End of Document ---_ 

Confidential — Uro Care HMS SaaS  |  Page _29_ 

