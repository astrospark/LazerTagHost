using LazerTagHostLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LazerTagHost_Test
{
    
    
    /// <summary>
    ///This is a test class for HexCodedDecimalTest and is intended
    ///to contain all HexCodedDecimalTest Unit Tests
    ///</summary>
	[TestClass()]
	public class HexCodedDecimalTest
	{


		private TestContext testContextInstance;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext
		{
			get
			{
				return testContextInstance;
			}
			set
			{
				testContextInstance = value;
			}
		}

		#region Additional test attributes
		// 
		//You can use the following additional attributes as you write your tests:
		//
		//Use ClassInitialize to run code before running the first test in the class
		//[ClassInitialize()]
		//public static void MyClassInitialize(TestContext testContext)
		//{
		//}
		//
		//Use ClassCleanup to run code after all tests in a class have run
		//[ClassCleanup()]
		//public static void MyClassCleanup()
		//{
		//}
		//
		//Use TestInitialize to run code before running each test
		//[TestInitialize()]
		//public void MyTestInitialize()
		//{
		//}
		//
		//Use TestCleanup to run code after each test has run
		//[TestCleanup()]
		//public void MyTestCleanup()
		//{
		//}
		//
		#endregion


		/// <summary>
		///A test for FromDecimal
		///</summary>
		[TestMethod()]
		public void FromDecimalTest()
		{
			Assert.AreEqual(HexCodedDecimal.FromDecimal(0), 0x0);
			Assert.AreEqual(HexCodedDecimal.FromDecimal(1), 0x1);
			Assert.AreEqual(HexCodedDecimal.FromDecimal(9), 0x9);
			Assert.AreEqual(HexCodedDecimal.FromDecimal(10), 0x10);
			Assert.AreEqual(HexCodedDecimal.FromDecimal(99), 0x99);
			Assert.AreEqual(HexCodedDecimal.FromDecimal(255), 0xff);
		}

		/// <summary>
		///A test for ToDecimal
		///</summary>
		[TestMethod()]
		public void ToDecimalTest()
		{
			Assert.AreEqual(HexCodedDecimal.ToDecimal(0x0), 0);
			Assert.AreEqual(HexCodedDecimal.ToDecimal(0x1), 1);
			Assert.AreEqual(HexCodedDecimal.ToDecimal(0x9), 9);
			Assert.AreEqual(HexCodedDecimal.ToDecimal(0x10), 10);
			Assert.AreEqual(HexCodedDecimal.ToDecimal(0x99), 99);
			Assert.AreEqual(HexCodedDecimal.ToDecimal(0xff), 255);
		}
	}
}
