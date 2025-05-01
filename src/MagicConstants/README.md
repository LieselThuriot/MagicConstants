# Magic Constants

Magic Constants is a dotnet source generator that generates C# constants from files in your project.
It can also minify files and set cache control headers for routes.

This is especially useful for web projects where you want to reference files in your code without hardcoding the paths nor having to manage the string constants.

Global settings in your project:

```xml
<PropertyGroup>
  <MagicConstantsVisibility>public</MagicConstantsVisibility>
  <MagicConstantsRoutes>true</MagicConstantsRoutes>
  <MagicConstantsRoutesCacheControl>public, max-age=604800</MagicConstantsRoutesCacheControl>
  <MagicConstantsMinify>true</MagicConstantsMinify>
</PropertyGroup>
```

Specific settings
( Note: if not specified, global settings will be used instead )

```xml
<ItemGroup>
  <AdditionalFiles Include="**\*.html" MagicClass="Pages" MagicRemoveRouteExtension="true" MagicCacheControl="public, max-age=86400" MagicMinify="true" />
  <AdditionalFiles Include="**\*.css" MagicClass="Assets" MagicMinify="true" />
  <AdditionalFiles Include="**\*.js" MagicClass="Assets" MagicMinify="true" />
  <AdditionalFiles Include="**\*.svg" MagicClass="Images" />
  <AdditionalFiles Include="**\*.png" MagicClass="Images" />
  <AdditionalFiles Include="**\*.ico" MagicClass="Images" />
</ItemGroup>
```

When `MagicConstantsRoutes` is enabled, it will generate an aspnet route for every file. All you have to do, is call the mapping method:

```csharp
app.MapViews();
```

Magic Constants also allows you to replace variables inside your .htm, .html, .css and .js files.
You can use the following syntax:
```html
<a href="index.html?noCache={MAGIC_TIME}">
<a href="index.html?noCache={MAGIC_HASH}">
{MAGIC_FILE test.html}
```

- MAGIC_TIME will use the Unix Timestamp in seconds during build, 
- MAGIC_HASH will try and make a unique but consistent hash out of the timestamp.
- MAGIC_FILE is a simplistic template inliner.

This will end up as:
```html
<a href="index.html?noCache=1746028631">
<a href="index.html?noCache=LHf1JfsN">
<div>This is the content of test.html</div>
```