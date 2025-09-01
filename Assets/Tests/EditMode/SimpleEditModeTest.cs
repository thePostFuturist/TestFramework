using NUnit.Framework;
using UnityEngine;
using PerSpec;
using System.Collections.Generic;
using System.Linq;

namespace Tests.EditMode
{
    /// <summary>
    /// Simple EditMode tests to verify the test framework and namespace resolution
    /// </summary>
    public class SimpleEditModeTest
    {
        [Test]
        public void Should_Pass_Basic_Math_Test()
        {
            // Arrange
            PerSpecDebug.LogTestSetup("Setting up basic math test in EditMode");
            int a = 10;
            int b = 20;
            int expected = 30;
            
            // Act
            PerSpecDebug.LogTestAction("Performing addition");
            int result = a + b;
            
            // Assert
            Assert.AreEqual(expected, result, "Basic addition should work");
            PerSpecDebug.LogTestComplete("Basic math test passed!");
        }
        
        [Test]
        public void Should_Manipulate_Strings_Correctly()
        {
            // Arrange
            PerSpecDebug.LogTestSetup("Testing string manipulation");
            string input = "perspec";
            string expected = "PERSPEC";
            
            // Act
            PerSpecDebug.LogTestAction("Converting to uppercase");
            string result = input.ToUpper();
            
            // Assert
            Assert.AreEqual(expected, result, "String should be converted to uppercase");
            PerSpecDebug.LogTestComplete("String manipulation test passed!");
        }
        
        [Test]
        public void Should_Work_With_Collections()
        {
            // Arrange
            PerSpecDebug.LogTestSetup("Testing collection operations");
            var numbers = new List<int> { 1, 2, 3, 4, 5 };
            int expectedSum = 15;
            int expectedCount = 5;
            
            // Act
            PerSpecDebug.LogTestAction("Calculating sum and count");
            int sum = numbers.Sum();
            int count = numbers.Count;
            
            // Assert
            Assert.AreEqual(expectedSum, sum, "Sum should be correct");
            Assert.AreEqual(expectedCount, count, "Count should be correct");
            PerSpecDebug.LogTestComplete("Collection test passed!");
        }
        
        [Test]
        public void Should_Verify_Unity_Math_Operations()
        {
            // Arrange
            PerSpecDebug.LogTestSetup("Testing Unity Vector3 operations");
            Vector3 a = new Vector3(1, 2, 3);
            Vector3 b = new Vector3(4, 5, 6);
            Vector3 expected = new Vector3(5, 7, 9);
            
            // Act
            PerSpecDebug.LogTestAction("Adding vectors");
            Vector3 result = a + b;
            
            // Assert
            Assert.AreEqual(expected, result, "Vector addition should work correctly");
            PerSpecDebug.LogTestComplete("Unity math test passed!");
        }
        
        [Test]
        public void Should_Handle_Null_Checks()
        {
            // Arrange
            PerSpecDebug.LogTestSetup("Testing null safety");
            string nullString = null;
            string validString = "PerSpec";
            
            // Act & Assert
            PerSpecDebug.LogTestAction("Checking null conditions");
            Assert.IsNull(nullString, "Null string should be null");
            Assert.IsNotNull(validString, "Valid string should not be null");
            Assert.IsTrue(string.IsNullOrEmpty(nullString), "Null string should be empty");
            Assert.IsFalse(string.IsNullOrEmpty(validString), "Valid string should not be empty");
            
            PerSpecDebug.LogTestComplete("Null check test passed!");
        }
        
        [Test]
        public void Should_Test_Boolean_Logic()
        {
            // Arrange
            PerSpecDebug.LogTestSetup("Testing boolean logic");
            bool trueValue = true;
            bool falseValue = false;
            
            // Act & Assert
            PerSpecDebug.LogTestAction("Testing logical operations");
            Assert.IsTrue(trueValue && true, "AND with true should be true");
            Assert.IsFalse(trueValue && falseValue, "AND with false should be false");
            Assert.IsTrue(trueValue || falseValue, "OR should be true if any is true");
            Assert.IsFalse(!trueValue, "NOT true should be false");
            
            PerSpecDebug.LogTestComplete("Boolean logic test passed!");
        }
        
        [Test]
        public void Should_Demonstrate_Test_Categories()
        {
            // This test demonstrates that we can run tests by namespace
            PerSpecDebug.LogTestSetup("Demonstrating namespace-based test execution");
            
            // Act
            string testNamespace = typeof(SimpleEditModeTest).FullName;
            PerSpecDebug.LogTestAction($"Test class full name: {testNamespace}");
            
            // Assert
            Assert.AreEqual("Tests.EditMode.SimpleEditModeTest", testNamespace, 
                "Namespace should match expected pattern");
            
            PerSpecDebug.LogTestComplete("Namespace verification passed!");
        }
    }
}