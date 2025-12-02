// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Files.InteractionTests.Tests
{
    [TestClass]
    public sealed class MiddleClickTabTests
    {
        [TestMethod]
        public void TabBarHasMiddleClickHandlerMethod()
        {
            // This is a light-weight, non-UI check to ensure the TabBar contains
            // a pointer released handler method for middle-click behavior.
            // The actual runtime behavior is UI-specific and needs to be validated
            // on Windows (UI tests). This test simply ensures the code member exists
            // and will be picked up by CI on Windows.

            // As the UI project targets Windows/WinUI, we cannot rely on loading the runtime
            // assembly from non-Windows environments. Instead we perform a source-level check
            // to ensure the handler method exists in the file. The runtime UI behavior should
            // still be validated by Windows UI tests.

            var repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
            var sourcePath = System.IO.Path.Combine(repoRoot, "src", "Files.App", "UserControls", "TabBar", "TabBar.xaml.cs");
            Assert.IsTrue(System.IO.File.Exists(sourcePath), $"Expected source file not found: {sourcePath}");

            var content = System.IO.File.ReadAllText(sourcePath);
            Assert.IsTrue(content.Contains("DragAreaRectangle_PointerReleased"), "The handler method name was not found in source file.");
        }
    }
}
