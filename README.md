# RoodFluweel.ValueObjects

A source generator to mark value objects in C#.

## Overview

**RoodFluweel.ValueObjects** is a C# source generator that helps you define and work with value objects in your domain model. It automatically generates equality, hash code, and other boilerplate code for your value objects, making your codebase cleaner and less error-prone.

- **Targets:** .NET Standard 2.0, .NET 8

## Why use this instead of C# records?

While C# records provide built-in value-based equality and are a great fit for value objects, they are only available in .NET 5.0 and later.  
**RoodFluweel.ValueObjects** uses a source generator approach, which works with older frameworks such as .NET Standard 2.0 and .NET Framework 4.8.  
This makes it possible to use modern value object patterns in legacy projects that cannot use records.

Other benefits:
- Works with both classes and structs.
- Allows for additional customization and features beyond what records provide.

## Installation

Add the NuGet package to your project:

dotnet add package RoodFluweel.ValueObjects

Or via the Package Manager:

Install-Package RoodFluweel.ValueObjects

## Usage

1. **Mark your class or struct as a value object:**

```csharp
[ValueObject]
public partial class EmailAddress
{
    public string Value { get; }
}
```

2. **Build your project.**  
   The source generator will automatically generate equality members and other value object boilerplate.

### Example

```csharp
[ValueObject]
public partial class EmailAddress
{
    public string Value { get; }
}
```

You can now use `EmailAddress` as a value object with value-based equality.

## How it works

- The `[ValueObject]` attribute triggers the source generator.
- The generator creates partial class implementations for equality, `GetHashCode`, and other standard value object patterns.

## Requirements

- C# 10.0 or later recommended
- .NET Standard 2.0 or .NET 8

## Contributing

Contributions are welcome! Please open issues or submit pull requests.

## License

This project is licensed under the MIT License.
