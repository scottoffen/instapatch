# InstaPatch

InstaPatch is a lightweight C# class library for implementing JSON Patch operations based on [RFC 6902](https://datatracker.ietf.org/doc/html/rfc6902). Designed with simplicity and flexibility in mind, InstaPatch supports validating patch operations outside the ASP.NET pipeline, while avoiding unnecessary dependencies.

## Features

**No Transitive Dependencies:**  
InstaPatch is built to keep the dependency graph minimal. It doesn't bring in any extra libraries that you don't need.

**Eliminate Newtonsoft.JSON Dependency:**  
By relying on built-in .NET JSON functionality (System.Text.Json), InstaPatch avoids the overhead of additional JSON libraries such as Newtonsoft.JSON.

**Externalizable Validation:**  
The library provides mechanisms to validate individual patch operations against your data model without being tied to the ASP.NET model binding and validation pipeline. This allows for more granular and flexible error handling.

**Attribute-based Configuration:**  
The `DenyPatchAttribute` marks types or properties that should not be patched, enabling developers to control patchability declaratively.

**Reflection-based Validation:**  
Validation leverages lazy-loading cached reflection to inspect the target model's properties. It builds collections of patchable properties and validates each patch operation against the model, returning clear error messages when a constraint is violated.

**Minimal and Focused Implementation:**  
InstaPatch is purposely minimalistic. It currently supports only top-level properties for patching operations. While RFC 6902 does support nested structures, this is a conscious decision to remain focused on the core use case without introducing additional complexity.