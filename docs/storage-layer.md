# A New Era for the Storage Layer in Files App

## Background
<!-- Use this section to provide background context for the new API(s) 
in this spec. -->

<!-- For example, this section is a place to explain why you're adding this API rather than
modifying an existing API. -->

<!-- For example, this is a place to provide a brief explanation of some dependent
area, just explanation enough to understand this new API, rather than telling
the reader "go read 100 pages of background information posted at ...". -->

Presently, it is common for users to browse a large folder of storage items, but only interact with a small subset of them, if any. Relatively recent expansions to the type of filesystem APIs allowed within an `AppContainer` (low-IL) context have improved the app experience for users by many orders of magnitude. Notably, telemetry data implies that app session durations are not only longer but also more inline with user expectations. Over the last three years, contributors have used this insight to justify significant investment into how Files enumerates storage items from the filesystem. 

One of the most consequential parts of this effort was the distinction between standard and extended item properties. Standard properties are those that Files can access quickly using the `FindFirstFileExFromApp()` method, and extended properties are those that Files cannot access quickly due to the required overhead of constructing an `IStorageItem` instance. Luckily, transitioning away from the bulk enumeration methods on `StorageFolder` resulted in significant performance improvements; a sizable reduction in memory usage; and the learning that users, at any given moment, are *almost never* concerned with every single storage item in the working directory-let alone every single property of each item in the viewport.

Thus, the decision was made that extended properties like the thumbnail, display type, and others should have their value availability deferred until the respective item container is scrolled into the viewport. While not all layout modes at the time included an `ItemsControl` such as `ListView` or `GridView`, this *lazy loading* scheme was enabled by particular UI virtualization events on the controls, which fire when the user scrolls new items into the viewport.

The Files developers are of the opinion that further improvements can be made to the storage layer.

## Motivation
The current storage layer that includes these prior improvements is very loosely organized, but one of its major characteristics is a largely unmaintainable monolith that lacks sound architecture patterns. Many intended concerns of this so-called `ItemViewModel` are poorly defined for the demands of a flexible, consistent and extensible storage layer.

Community ambitions to deliver a high-quality file manager experience wherever users may be also catalyze this effort, for the necessary ability to display items from multiple non-native sources requires careful planning.

Lastly, the current *lazy loading* pattern was introduced at a time before all layout modes used `ItemsControl`s, and thus could not take advantage of virtualization at the data source level.

## Description
<!-- Use this section to provide a brief description of the feature.
For an example, see the introduction to the PasswordBox control 
(http://learn.microsoft.com/windows/uwp/design/controls-and-patterns/password-box). -->


## Examples
<!-- Use this section to explain the features of the API, showing
example code with each description. The general format is: 
  feature explanation,
  example code
  feature explanation,
  example code
  etc.-->
  
<!-- Code samples should be in C# and/or C++/WinRT -->

<!-- As an example of this section, see the Examples section for the PasswordBox control 
(https://learn.microsoft.com/windows/uwp/design/controls-and-patterns/password-box#examples). -->


## Remarks
<!-- Explanation and guidance that doesn't fit into the Examples section. -->

<!-- APIs should only throw exceptions in exceptional conditions; basically,
only when there's a bug in the caller, such as argument exception.  But if for some
reason it's necessary for a caller to catch an exception from an API, call that
out with an explanation either here or in the Examples -->

## API Notes
<!-- Option 1: Give a one or two line description of each API (type
and member), or at least the ones that aren't obvious
from their name.  These descriptions are what show up
in IntelliSense. For properties, specify the default value of the property if it
isn't the type's default (for example an int-typed property that doesn't default to zero.) -->

<!-- Option 2: Put these descriptions in the below API Details section,
with a "///" comment above the member or type. -->

## API Details
<!-- The exact API, in MIDL3 format (https://learn.microsoft.com/uwp/midl-3/) -->

## Appendix
<!-- Anything else that you want to write down for posterity, but 
that isn't necessary to understand the purpose and usage of the API.
For example, implementation details. -->