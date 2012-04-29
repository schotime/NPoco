NPoco
=====

Welcome to the NPoco! NPoco is a fork of PetaPoco based on Schotime's branch with a handful of extra features.

### Getting Started: Your first query

```csharp
public class User 
{
    public int UserId { get;set; }
    public string Email { get;set; }
}

IDatabase db = new Database("connStringName");
List<User> users = db.Fetch<User>("select userId, email from users");
```

This works by mapping the column names to the property names on the ``User`` object. This is a case-insensitive match.  
There is no mapping setup needed for this (query only) scenario. 

Checkout the [Wiki](https://github.com/schotime/NPoco/wiki/Home) for more documentation.