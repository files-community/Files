using NUnit.Framework;
using Files;
using Files.Views;

namespace TestProjectFiles
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }
        /// <summary>
        /// Testing for Details instantiation
        /// </summary>
        [Test]
        public void TestDetailsInstantiation()
        {
            PropertiesDetails propertiesDetails = new PropertiesDetails();
            Assert.IsNotNull(propertiesDetails);
        }
        /// <summary>
        /// Testing for Details spacing
        /// </summary>
        [Test]
        public void TestDetailsCorrectSpacing()
        {
            PropertiesDetails propertiesDetails = new PropertiesDetails();
            var testSpacing = propertiesDetails.ViewModel.ValueSpacing;
            Assert.AreEqual(testSpacing, "150");
        }
        /// <summary>
        /// Testing for Details spacing
        /// </summary>
        [Test]
        public void TestDetailsCorrectNotSpacing()
        {
            PropertiesDetails propertiesDetails = new PropertiesDetails();
            var testSpacing = propertiesDetails.ViewModel.ValueSpacing;
            Assert.AreNotEqual(testSpacing, "*");
        }
    }
}