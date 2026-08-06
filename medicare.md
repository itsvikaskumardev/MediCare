Viewed Doctor.cs:1-60

The difference comes down to which side of a **One-to-Many (1 : N)** relationship you are modeling in Entity Framework Core (EF Core). 

In relational databases, relationships have two sides: the **Principal ("One" / Parent)** side and the **Dependent ("Many" / Child)** side.

---

### 1. The Two Types of Navigation Properties

| Property Type | Syntax Example | Meaning in EF Core |
|---|---|---|
| **Collection Navigation Property** (`ICollection<T>`) | `public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();` | **The "One" (Parent) side:** <br>• 1 Doctor has **many** Appointments.<br>• 1 Category has **many** MenuItems. |
| **Reference Navigation Property** (`T?`) | `public Category? Category { get; set; }`<br>`public Doctor? Doctor { get; set; }` | **The "Many" (Child / Dependent) side:** <br>• 1 MenuItem belongs to **one** Category.<br>• 1 Appointment belongs to **one** Doctor. |

---

### 2. When to Use Which Approach

#### Use `ICollection<T>` when:
* **One entity owns or can contain multiple related entities.**
* Example in Medicare: A `Doctor` can have dozens of `Appointments`.
* Example in a Restaurant API: A `Category` (e.g., "Beverages") contains multiple `MenuItems`.

```csharp
// The "One" / Parent side
public class Doctor 
{
    public Guid Id { get; set; }
    
    // 1 Doctor -> N Appointments
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
```

> [!TIP]
> **Why initialize with `= new List<T>()`?**
> Initializing the collection prevents a `NullReferenceException` if you create a new `Doctor` in memory and immediately call `doctor.Appointments.Add(newAppointment)` before saving to the database.

---

#### Use a Single Reference Property (`T?`) when:
* **An entity holds a foreign key pointing to a single parent entity.**
* Example in Medicare: A single `Appointment` is booked with **one** specific `Doctor`.
* Example in a Restaurant API: A `MenuItem` belongs to **one** specific `Category`.

```csharp
// The "Many" / Child side
public class MenuItem 
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Foreign Key column in the table
    public Guid CategoryId { get; set; }

    // Reference Navigation Property (1 MenuItem -> 1 Category)
    public Category? Category { get; set; }
}
```

---

### 3. How They Work Together (Bi-Directional Relationships)

Most of the time in EF Core, you use **both** on opposite sides of the relationship so that you can navigate in either direction:

```csharp
// 1. Doctor (Parent) has an ICollection of Appointments
public class Doctor 
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}

// 2. Appointment (Child) has a single reference back to Doctor
public class Appointment 
{
    public Guid Id { get; set; }

    public Guid DoctorId { get; set; }     // Foreign Key
    public Doctor? Doctor { get; set; }    // Single Reference Navigation Property
}
```

* **Querying from the Parent (`ICollection<T>`):**  
  `var doctor = await db.Doctors.Include(d => d.Appointments).FirstOrDefaultAsync(...);`  
  Gives you the doctor and all of their scheduled appointments.
* **Querying from the Child (`T?`):**  
  `var appointment = await db.Appointments.Include(a => a.Doctor).FirstOrDefaultAsync(...);`  
  Gives you the appointment along with the profile details of the doctor assigned to it.
---

**No, you cannot swap them interchangeably on the same class**, because EF Core uses the property type to decide what SQL tables and foreign keys to create in your database:

* If you put `public Doctor? Doctor { get; set; }` in **`Appointment`**, EF Core knows: *"1 Appointment has **1 Doctor**."*
* If you mistakenly put `public ICollection<Doctor> Doctors { get; set; }` in **`Appointment`**, EF Core will think: *"1 Appointment can have **many Doctors**"* and will create a Many-to-Many join table in SQL!

---

### What You *CAN* Choose: How Many Sides to Include

While you cannot swap their types, **you do NOT have to write both properties in your project.** EF Core allows you to choose between **Uni-directional** (one side only) or **Bi-directional** (both sides).

#### 1. Reference Property Only (Very Common in Minimal APIs / DTOs)
You only put the single reference property on the child class and **omit** `ICollection<T>` from the parent class completely:
```csharp
// Doctor.cs — Clean! No ICollection here.
public class Doctor {
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// Appointment.cs
public class Appointment {
    public Guid Id { get; set; }
    public Guid DoctorId { get; set; }
    public Doctor? Doctor { get; set; } // Only this!
}
```
* **Why people do this:** 
  1. It prevents **Circular Reference / JSON serialization errors** in Web APIs (where `Doctor` serializes `Appointments`, which serializes `Doctor`, which serializes `Appointments` infinitely).
  2. If you want a Doctor's appointments, you just query directly:  
     `db.Appointments.Where(a => a.DoctorId == doctorId).ToListAsync()`

---

#### 2. Both Properties (Bi-Directional)
You include **both** properties on opposite classes:
```csharp
// Doctor.cs
public class Doctor {
    public Guid Id { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}

// Appointment.cs
public class Appointment {
    public Guid Id { get; set; }
    public Guid DoctorId { get; set; }
    public Doctor? Doctor { get; set; }
}
```
* **Why people do this:** 
  1. When you want to eagerly load everything in one query using `.Include()`:  
     `var doctor = await db.Doctors.Include(d => d.Appointments).FirstOrDefaultAsync(...);`

---

### Summary Recommendation for Your Project
* **For `Appointment` (The Child):** Always use a **Reference Navigation Property** (`public Doctor? Doctor { get; set; }`) along with its foreign key (`DoctorId`).
* **For `Doctor` (The Parent):** Adding `ICollection<Appointment> Appointments` is **optional**. In Minimal APIs, many developers leave it out to keep models clean and avoid JSON serialization loops.

---

---

---
Viewed ServiceAppointmentService.cs:38-70

In line 60 (`services.FirstOrDefault(s => s.Id == a.ServiceId)`), **`FirstOrDefault`** is used because `services` is already an **in-memory C# List**, not a database query.

Here is the breakdown of when to use which and why:

---

### 1. When to use `FirstOrDefaultAsync` (Database Queries)
Use **`FirstOrDefaultAsync`** when querying Entity Framework (`_db.Table...` / `IQueryable`):
```csharp
// Makes a network/database call -> MUST be async
var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == id);
```
* **Why:** Calling a database is an **I/O operation** (network/disk access). Async keeps the server thread free to handle other requests while waiting for the database.

---

### 2. When to use `FirstOrDefault` (In-Memory Lists)
Use **`FirstOrDefault`** when searching a regular C# collection or `List<T>` that is already loaded in memory:
```csharp
// Step 1: Fetch from DB into memory (async)
var services = await _db.Services.Where(...).ToListAsync();

// Step 2: Search the in-memory list (sync)
var svc = services.FirstOrDefault(s => s.Id == a.ServiceId);
```
* **Why:** 
  1. **Not I/O bound:** Searching an in-memory list takes microseconds (CPU-bound), so async overhead is unnecessary.
  2. **Not supported on lists:** LINQ to Objects (`IEnumerable<T>` / `List<T>`) does not have a `.FirstOrDefaultAsync()` extension method—it only exists for Entity Framework (`IQueryable<T>`).

---

### Summary: Which is better?
Both are best practice **in their respective contexts**:
* **`FirstOrDefaultAsync`**: Best for **Database (`_db.Table`)** queries.
* **`FirstOrDefault`**: Best for **In-Memory (`List<T>`)** lookups.

---
---
---
In DoctorService.cs

There are two different C# rules at play here that are causing this behavior. 

### 1. Why we use `=` instead of just `using`
In C#, a standard `using` statement (like `using backend_dotnet.Models.Domain;`) can **only import namespaces**, not individual classes. 

If you try to write `using backend_dotnet.Models.Domain.User;`, the C# compiler will throw an error because `User` is a class, not a namespace. To target a specific class to resolve a conflict, C# requires you to create an alias using the `=` sign: 
`using User = backend_dotnet.Models.Domain.User;`

### 2. Why it had to be *inside* the namespace block
Normally, you *can* put `using User = ...` at the very top of the file! However, your project has a specific naming collision. 

You have a folder/namespace in your project called `backend_dotnet.Services.User`. 
Your doctor service is inside `backend_dotnet.Services.Doctor`. 

Because they share the same parent (`backend_dotnet.Services`), they are "sibling" namespaces. The C# compiler has a quirky rule: **it gives priority to sibling namespaces over global `using` aliases at the top of the file.** 

So, if you put the alias at the top of the file, the compiler ignores it, looks at the word `User` in your code, and says *"Ah, you must mean your sibling namespace `backend_dotnet.Services.User`!"* (which causes the "used like a type" error).

By moving the alias **inside** the `namespace { ... }` block, you flip the priority. You are explicitly telling the compiler: *"Whenever you see `User` inside this specific block of code, I don't care about the sibling namespace, I want you to use the Domain Model."*

*(Note: I noticed you removed the inner alias in your last file edit! If you try to run `dotnet build` right now, that exact same error will pop up again. You'll need to put `using User = backend_dotnet.Models.Domain.User;` back inside the namespace block to make it compile).*