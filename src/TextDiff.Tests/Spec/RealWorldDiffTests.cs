using Xunit.Abstractions;

namespace TextDiff.Tests.Spec;

/// <summary>
/// Real-world diff scenario tests simulating actual git diff outputs
/// and complex editing patterns.
/// </summary>
public class RealWorldDiffTests
{
    private readonly TextDiffer _differ = new();
    private readonly ITestOutputHelper _output;

    public RealWorldDiffTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Git Diff Scenarios

    [Fact]
    public void Process_GitDiffWithFullHeaders_ShouldApplyCorrectly()
    {
        // Arrange - Full git diff output format
        var document = @"namespace MyApp
{
    public class Calculator
    {
        public int Add(int a, int b)
        {
            return a + b;
        }
    }
}";

        var diff = @"diff --git a/Calculator.cs b/Calculator.cs
index abc1234..def5678 100644
--- a/Calculator.cs
+++ b/Calculator.cs
@@ -1,10 +1,15 @@
 namespace MyApp
 {
     public class Calculator
     {
         public int Add(int a, int b)
         {
             return a + b;
         }
+
+        public int Subtract(int a, int b)
+        {
+            return a - b;
+        }
     }
 }";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("Subtract", result.Text);
        Assert.Contains("return a - b;", result.Text);
    }

    [Fact]
    public void Process_MultipleFileChangesInDocument_ShouldApplyRelevantChanges()
    {
        // Arrange - Simulating a single file being modified in multiple places
        var document = @"using System;

namespace MyApp
{
    public class Service
    {
        private readonly ILogger _logger;

        public Service(ILogger logger)
        {
            _logger = logger;
        }

        public void Process()
        {
            _logger.Log(""Processing..."");
        }

        public void Complete()
        {
            _logger.Log(""Done"");
        }
    }
}";

        var diff = @"@@ -6,7 +6,8 @@
     public class Service
     {
         private readonly ILogger _logger;
+        private readonly IConfig _config;

         public Service(ILogger logger)
         {
@@ -14,7 +15,7 @@

         public void Process()
         {
-            _logger.Log(""Processing..."");
+            _logger.LogInfo(""Processing started"");
         }

         public void Complete()";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("private readonly IConfig _config;", result.Text);
        Assert.Contains("LogInfo", result.Text);
        Assert.Contains("Processing started", result.Text);
    }

    #endregion

    #region Code Refactoring Scenarios

    [Fact]
    public void Process_MethodRename_ShouldApplyCorrectly()
    {
        // Arrange
        var document = @"public class UserService
{
    public User GetUser(int id)
    {
        return _repository.Find(id);
    }

    public void DeleteUser(int id)
    {
        _repository.Delete(id);
    }
}";

        var diff = @" public class UserService
 {
-    public User GetUser(int id)
+    public User FindUserById(int id)
     {
         return _repository.Find(id);
     }

-    public void DeleteUser(int id)
+    public void RemoveUser(int id)
     {
         _repository.Delete(id);
     }
 }";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("FindUserById", result.Text);
        Assert.Contains("RemoveUser", result.Text);
        Assert.DoesNotContain("GetUser", result.Text);
        Assert.DoesNotContain("DeleteUser", result.Text);
    }

    [Fact]
    public void Process_AddImportsAndModifyCode_ShouldApplyBoth()
    {
        // Arrange
        var document = @"using System;

namespace MyApp
{
    public class Handler
    {
        public void Handle()
        {
            Console.WriteLine(""Handling"");
        }
    }
}";

        var diff = @" using System;
+using System.Threading.Tasks;
+using Microsoft.Extensions.Logging;

 namespace MyApp
 {
     public class Handler
     {
-        public void Handle()
+        public async Task HandleAsync()
         {
-            Console.WriteLine(""Handling"");
+            await Task.Delay(100);
+            _logger.LogInformation(""Handling"");
         }
     }
 }";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("using System.Threading.Tasks;", result.Text);
        Assert.Contains("using Microsoft.Extensions.Logging;", result.Text);
        Assert.Contains("async Task HandleAsync", result.Text);
        Assert.Contains("await Task.Delay", result.Text);
    }

    #endregion

    #region JSON/Config File Changes

    [Fact]
    public void Process_JsonConfigChange_ShouldPreserveFormatting()
    {
        // Arrange
        var document = @"{
  ""name"": ""my-app"",
  ""version"": ""1.0.0"",
  ""dependencies"": {
    ""lodash"": ""^4.17.0""
  }
}";

        var diff = @" {
   ""name"": ""my-app"",
-  ""version"": ""1.0.0"",
+  ""version"": ""1.1.0"",
   ""dependencies"": {
-    ""lodash"": ""^4.17.0""
+    ""lodash"": ""^4.17.21"",
+    ""axios"": ""^1.0.0""
   }
 }";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("\"version\": \"1.1.0\"", result.Text);
        Assert.Contains("\"lodash\": \"^4.17.21\"", result.Text);
        Assert.Contains("\"axios\": \"^1.0.0\"", result.Text);
    }

    [Fact]
    public void Process_YamlConfigChange_ShouldPreserveIndentation()
    {
        // Arrange
        var document = @"server:
  host: localhost
  port: 3000
database:
  host: localhost
  port: 5432";

        var diff = @" server:
   host: localhost
-  port: 3000
+  port: 8080
 database:
-  host: localhost
+  host: db.example.com
   port: 5432";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("port: 8080", result.Text);
        Assert.Contains("host: db.example.com", result.Text);
    }

    #endregion

    #region Large File Scenarios

    [Fact]
    public void Process_LargeFileWithScatteredChanges_ShouldApplyAllChanges()
    {
        // Arrange - 1000 line file with changes at various positions
        var lines = Enumerable.Range(1, 1000).Select(i => $"line_{i:D4}").ToList();
        var document = string.Join("\n", lines);

        var diff = @"@@ -10,3 +10,3 @@
 line_0010
-line_0011
+MODIFIED_0011
 line_0012
@@ -500,3 +500,4 @@
 line_0500
-line_0501
+MODIFIED_0501
+INSERTED_LINE
 line_0502
@@ -998,3 +999,3 @@
 line_0998
 line_0999
-line_1000
+MODIFIED_1000";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("MODIFIED_0011", result.Text);
        Assert.Contains("MODIFIED_0501", result.Text);
        Assert.Contains("INSERTED_LINE", result.Text);
        Assert.Contains("MODIFIED_1000", result.Text);
    }

    [Fact]
    public void Process_ManySmallHunks_ShouldApplyAllCorrectly()
    {
        // Arrange - Many small changes throughout the file
        var lines = Enumerable.Range(1, 50).Select(i => $"line{i}").ToList();
        var document = string.Join("\n", lines);

        var diffBuilder = new System.Text.StringBuilder();
        // Create hunks at positions 5, 15, 25, 35, 45
        foreach (var pos in new[] { 5, 15, 25, 35, 45 })
        {
            diffBuilder.AppendLine($"@@ -{pos},3 +{pos},3 @@");
            diffBuilder.AppendLine($" line{pos}");
            diffBuilder.AppendLine($"-line{pos + 1}");
            diffBuilder.AppendLine($"+CHANGED_{pos + 1}");
            diffBuilder.AppendLine($" line{pos + 2}");
        }

        // Act
        var result = _differ.Process(document, diffBuilder.ToString());

        // Assert
        Assert.Contains("CHANGED_6", result.Text);
        Assert.Contains("CHANGED_16", result.Text);
        Assert.Contains("CHANGED_26", result.Text);
        Assert.Contains("CHANGED_36", result.Text);
        Assert.Contains("CHANGED_46", result.Text);
        Assert.Equal(5, result.Changes.ChangedLines);
    }

    #endregion

    #region Edge Cases from Real Diffs

    [Fact]
    public void Process_AddEmptyMethod_ShouldApplyCorrectly()
    {
        // Arrange
        var document = @"public class MyClass
{
    public void ExistingMethod()
    {
    }
}";

        var diff = @" public class MyClass
 {
     public void ExistingMethod()
     {
     }
+
+    public void NewMethod()
+    {
+    }
 }";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("NewMethod", result.Text);
    }

    [Fact]
    public void Process_RemoveAndReaddWithModification_ShouldApplyCorrectly()
    {
        // Arrange - Common pattern when reformatting
        // Note: This library uses "prefix + space + content" format
        // So "+ content" means content is "content", not " content"
        var document = @"if (condition) { doSomething(); }";

        var diff = @"- if (condition) { doSomething(); }
+ if (condition)
+ {
+     doSomething();
+ }";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        var normalizedResult = result.Text.Replace("\r\n", "\n");
        Assert.Equal("if (condition)\n{\n    doSomething();\n}", normalizedResult);
    }

    [Fact]
    public void Process_CommentChanges_ShouldApplyCorrectly()
    {
        // Arrange
        var document = @"// Old comment
public void Method()
{
    // TODO: implement
}";

        var diff = @"-// Old comment
+// New comment describing the method
+// Added by: developer@example.com
 public void Method()
 {
-    // TODO: implement
+    // Implementation complete
+    DoWork();
 }";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("New comment", result.Text);
        Assert.Contains("developer@example.com", result.Text);
        Assert.Contains("Implementation complete", result.Text);
        Assert.Contains("DoWork();", result.Text);
    }

    #endregion

    #region Merge Conflict-like Patterns

    [Fact]
    public void Process_ConflictMarkerContent_ShouldTreatAsRegularContent()
    {
        // Arrange - Content that looks like merge conflict markers
        var document = @"normal line
<<<<<<< HEAD
conflict content
=======
other content
>>>>>>> branch
normal line";

        var diff = @" normal line
-<<<<<<< HEAD
-conflict content
-=======
-other content
->>>>>>> branch
+resolved content
 normal line";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("resolved content", result.Text);
        Assert.DoesNotContain("<<<<<<<", result.Text);
        Assert.DoesNotContain(">>>>>>>", result.Text);
    }

    #endregion

    #region Whitespace-Only Changes

    [Fact]
    public void Process_IndentationChange_ShouldPreserveOriginalIndentation()
    {
        // Arrange - The library preserves original indentation for changed lines
        // This is by design: changed lines keep original indentation, only content changes
        var document = "    four_spaces\n\tone_tab";
        var diff = @"- four_spaces
+ new_content
 	one_tab";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        // Original indentation (4 spaces) is preserved, only content changes
        Assert.Contains("    new_content", result.Text);
        Assert.Contains("\tone_tab", result.Text);
    }

    [Fact]
    public void Process_TrailingWhitespaceRemoval_ShouldApplyCorrectly()
    {
        // Arrange
        var document = "line with trailing   \nanother line";
        var diff = @"-line with trailing
+line with trailing
 another line";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("line with trailing\n", result.Text.Replace("\r\n", "\n"));
    }

    #endregion
}
