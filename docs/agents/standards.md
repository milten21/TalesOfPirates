# Coding standards

The Standards axis of a code review checks changed code against this file.

## How to read this document

Each guideline starts with one of these words:

**DO** guidelines describe practices that should always be followed. There will almost never be a valid reason to stray from them.
**DON'T** guidelines are the converse: things that are almost never a good idea. Hopefully, we don't have as many of these as other languages do because we have less historical baggage.
**PREFER** guidelines are practices that you should follow. However, there may be circumstances where it makes sense to do otherwise. Just make sure you understand the full implications of ignoring the guideline when you do.
**AVOID** guidelines are the dual to "prefer": stuff you shouldn't do but where there may be good reasons to on rare occasions.
**CONSIDER** guidelines are practices that you might or might not want to follow, depending on circumstances, precedents, and your own preference.

## Naming

### General

**DO** choose easily readable identifier names.
**DO** favor readability over brevity.
**DO NOT** use abbreviations or contractions as part of identifier names. For example, use GetWindow rather than GetWin.
**DO NOT** use any acronyms that are not widely accepted, and even if they are, only when necessary.

### Classes, Structs, and Interfaces

**DO** name classes and structs with nouns or noun phrases.
**DO** name interfaces with adjective phrases, or occasionally with nouns or noun phrases.
**CONSIDER** ending the name of derived classes with the name of the base class.

### Generic type parameters

**DO** name generic type parameters with descriptive names unless a single-letter name is completely self-explanatory
and a descriptive name would not add value.
**CONSIDER** using T as the type parameter name for types with one single-letter type parameter

### Enums

**DO** use a singular type name for an enumeration unless its values are bit fields.
**DO** use a plural type name for an enumeration with bit fields as values, also called flags enum.
**DO NOT** use an "Enum" suffix in enum type names.
**DO NOT** use "Flag" or "Flags" suffixes in enum type names.

### Methods

**DO** Give methods names that are verbs or verb phrases.

### Properties

**DO** name properties using a noun, noun phrase, or adjective.
**DON'T** have properties that match the name of "Get" methods as in the following example:
`public string TextWriter { get {...} set {...} } public string GetTextWriter(int value) { ... }`
**DO** name collection properties with a plural phrase describing the items in the collection instead of using a singular phrase followed by "List" or "Collection".
**DO** name Boolean properties with an affirmative phrase (CanSeek instead of CantSeek).
Optionally, you can also prefix Boolean properties with "Is", "Can", or "Has", but only where it adds value.
**CONSIDER** giving a property the same name as its type.
For example, the following property correctly gets and sets an enum value named Color, so the property is named Color:

```csharp
public enum Color {...}

public class Control {
    public Color Color { get {...} set {...} }
}
```

### Fields

**DO** name fields using a noun, noun phrase, or adjective.

### Parameters

**DO** use descriptive parameter names.
**CONSIDER** using names based on a parameter’s meaning rather than the parameter’s type.

## Architecture

**AVOID** `sealed` modifiers, use only where a type has a real inheritance hazard.

## Porting

`sources/` of the reference repo is one server build, not the protocol. Servers on the original protocol differ from
build to build.

**DO** keep a value as a parameter, with the reference build's value as the default, where the original reads it from
a config or takes it as a function argument. A compile-time `enum` or a literal in the client's own constructor is
fixed. "Every call site passes X" proves nothing.

## Code comments

**PREFER** using // TODO comments to mark code that needs to be revisited.
**AVOID** writing code comments. Code must be self-documenting.
