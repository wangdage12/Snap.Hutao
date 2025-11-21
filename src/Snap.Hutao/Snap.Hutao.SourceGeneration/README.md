# Snap.Hutao.SourceGeneration

> 生成器包的备份，目前还可以从nuget上获取，所以暂时不需要使用该目录  
> https://www.nuget.org/packages/Snap.Hutao.SourceGeneration/1.3.14

Source Code Generator for Snap.Hutao

# Development Guideline

Use https://roslynquoter.azurewebsites.net/ to get SyntaxTree

1. Every `IncrementalValue(s)Provider<T>`'s step result should be an `IEquatable<T>` to make it really becomes incremental.
2. So the intermediate models should be a `record (class/struct)` if possible
3. Intermediate array/enumerable should be a `ImmutableArray<T>` if possible, the pipeline use IA internally and has special check for it.
