using LazerTagHostLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LazerTagHost_Test
{
    
    
    /// <summary>
    ///This is a test class for TeamPlayerIdTest and is intended
    ///to contain all TeamPlayerIdTest Unit Tests
    ///</summary>
	[TestClass()]
	public class TeamPlayerIdTest
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
		///A test for TeamPlayerId Constructor
		///</summary>
		[TestMethod()]
		public void TeamPlayerIdConstructorTest()
		{
			var target = new TeamPlayerId(0, 0);
			Assert.AreEqual(0, target.PlayerNumber);
			Assert.AreEqual(0, target.TeamNumber);
			Assert.AreEqual(0, target.TeamPlayerNumber);
			Assert.AreEqual(0x0, target.Packed23);
			Assert.AreEqual(0x0, target.Packed34);
			Assert.AreEqual(0x0, target.Packed44);

			target = new TeamPlayerId(1, 1);
			Assert.AreEqual(1, target.PlayerNumber);
			Assert.AreEqual(1, target.TeamNumber);
			Assert.AreEqual(1, target.TeamPlayerNumber);
			Assert.AreEqual(0x8, target.Packed23);
			Assert.AreEqual(0x10, target.Packed34);
			Assert.AreEqual(0x10, target.Packed44); 

			target = new TeamPlayerId(1, 8);
			Assert.AreEqual(8, target.PlayerNumber);
			Assert.AreEqual(1, target.TeamNumber);
			Assert.AreEqual(8, target.TeamPlayerNumber);
			Assert.AreEqual(0xf, target.Packed23);
			Assert.AreEqual(0x17, target.Packed34);
			Assert.AreEqual(0x17, target.Packed44); 

			target = new TeamPlayerId(2, 1);
			Assert.AreEqual(9, target.PlayerNumber);
			Assert.AreEqual(2, target.TeamNumber);
			Assert.AreEqual(1, target.TeamPlayerNumber);
			Assert.AreEqual(0x10, target.Packed23);
			Assert.AreEqual(0x20, target.Packed34);
			Assert.AreEqual(0x20, target.Packed44);

			target = new TeamPlayerId(2, 8);
			Assert.AreEqual(16, target.PlayerNumber);
			Assert.AreEqual(2, target.TeamNumber);
			Assert.AreEqual(8, target.TeamPlayerNumber);
			Assert.AreEqual(0x17, target.Packed23);
			Assert.AreEqual(0x27, target.Packed34);
			Assert.AreEqual(0x27, target.Packed44);

			target = new TeamPlayerId(3, 1);
			Assert.AreEqual(17, target.PlayerNumber);
			Assert.AreEqual(3, target.TeamNumber);
			Assert.AreEqual(1, target.TeamPlayerNumber);
			Assert.AreEqual(0x18, target.Packed23);
			Assert.AreEqual(0x30, target.Packed34);
			Assert.AreEqual(0x30, target.Packed44);

			target = new TeamPlayerId(3, 8);
			Assert.AreEqual(24, target.PlayerNumber);
			Assert.AreEqual(3, target.TeamNumber);
			Assert.AreEqual(8, target.TeamPlayerNumber);
			Assert.AreEqual(0x1f, target.Packed23);
			Assert.AreEqual(0x37, target.Packed34);
			Assert.AreEqual(0x37, target.Packed44);
		}

		/// <summary>
		///A test for TeamPlayerId Constructor
		///</summary>
		[TestMethod()]
		public void TeamPlayerIdConstructorTest1()
		{
			var target = new TeamPlayerId(0);
			Assert.AreEqual(0, target.PlayerNumber);
			Assert.AreEqual(0, target.TeamNumber);
			Assert.AreEqual(0, target.TeamPlayerNumber);
			Assert.AreEqual(0x0, target.Packed23);
			Assert.AreEqual(0x0, target.Packed34);
			Assert.AreEqual(0x0, target.Packed44);

			target = new TeamPlayerId(1);
			Assert.AreEqual(1, target.PlayerNumber);
			Assert.AreEqual(1, target.TeamNumber);
			Assert.AreEqual(1, target.TeamPlayerNumber);
			Assert.AreEqual(0x8, target.Packed23);
			Assert.AreEqual(0x10, target.Packed34);
			Assert.AreEqual(0x10, target.Packed44); 

			target = new TeamPlayerId(8);
			Assert.AreEqual(8, target.PlayerNumber);
			Assert.AreEqual(1, target.TeamNumber);
			Assert.AreEqual(8, target.TeamPlayerNumber);
			Assert.AreEqual(0xf, target.Packed23);
			Assert.AreEqual(0x17, target.Packed34);
			Assert.AreEqual(0x17, target.Packed44); 

			target = new TeamPlayerId(9);
			Assert.AreEqual(9, target.PlayerNumber);
			Assert.AreEqual(2, target.TeamNumber);
			Assert.AreEqual(1, target.TeamPlayerNumber);
			Assert.AreEqual(0x10, target.Packed23);
			Assert.AreEqual(0x20, target.Packed34);
			Assert.AreEqual(0x20, target.Packed44);

			target = new TeamPlayerId(16);
			Assert.AreEqual(16, target.PlayerNumber);
			Assert.AreEqual(2, target.TeamNumber);
			Assert.AreEqual(8, target.TeamPlayerNumber);
			Assert.AreEqual(0x17, target.Packed23);
			Assert.AreEqual(0x27, target.Packed34);
			Assert.AreEqual(0x27, target.Packed44);

			target = new TeamPlayerId(17);
			Assert.AreEqual(17, target.PlayerNumber);
			Assert.AreEqual(3, target.TeamNumber);
			Assert.AreEqual(1, target.TeamPlayerNumber);
			Assert.AreEqual(0x18, target.Packed23);
			Assert.AreEqual(0x30, target.Packed34);
			Assert.AreEqual(0x30, target.Packed44);

			target = new TeamPlayerId(24);
			Assert.AreEqual(24, target.PlayerNumber);
			Assert.AreEqual(3, target.TeamNumber);
			Assert.AreEqual(8, target.TeamPlayerNumber);
			Assert.AreEqual(0x1f, target.Packed23);
			Assert.AreEqual(0x37, target.Packed34);
			Assert.AreEqual(0x37, target.Packed44);
		}

		/// <summary>
		///A test for FromPacked23
		///</summary>
		[TestMethod()]
		public void FromPacked23Test()
		{
			var expected = new TeamPlayerId(0);
			var actual = TeamPlayerId.FromPacked23(0x0);
			Assert.AreEqual(expected, actual);

			expected = new TeamPlayerId(1);
			actual = TeamPlayerId.FromPacked23(0x8);
			Assert.AreEqual(expected, actual);

			expected = new TeamPlayerId(8);
			actual = TeamPlayerId.FromPacked23(0xf);
			Assert.AreEqual(expected, actual);

			expected = new TeamPlayerId(9);
			actual = TeamPlayerId.FromPacked23(0x10);
			Assert.AreEqual(expected, actual);

			expected = new TeamPlayerId(16);
			actual = TeamPlayerId.FromPacked23(0x17);
			Assert.AreEqual(expected, actual);

			expected = new TeamPlayerId(17);
			actual = TeamPlayerId.FromPacked23(0x18);
			Assert.AreEqual(expected, actual);

			expected = new TeamPlayerId(24);
			actual = TeamPlayerId.FromPacked23(0x1f);
			Assert.AreEqual(expected, actual);
		}

		/// <summary>
		///A test for FromPacked34
		///</summary>
		[TestMethod()]
		public void FromPacked34Test()
		{
			var expected = new TeamPlayerId(0);
			var actual = TeamPlayerId.FromPacked34(0x0);
			Assert.AreEqual(expected, actual);

			expected = new TeamPlayerId(1);
			actual = TeamPlayerId.FromPacked34(0x10);
			Assert.AreEqual(expected, actual);

			expected = new TeamPlayerId(8);
			actual = TeamPlayerId.FromPacked34(0x17);
			Assert.AreEqual(expected, actual);

			expected = new TeamPlayerId(9);
			actual = TeamPlayerId.FromPacked34(0x20);
			Assert.AreEqual(expected, actual);

			expected = new TeamPlayerId(16);
			actual = TeamPlayerId.FromPacked34(0x27);
			Assert.AreEqual(expected, actual);

			expected = new TeamPlayerId(17);
			actual = TeamPlayerId.FromPacked34(0x30);
			Assert.AreEqual(expected, actual);

			expected = new TeamPlayerId(24);
			actual = TeamPlayerId.FromPacked34(0x37);
			Assert.AreEqual(expected, actual);
		}

		/// <summary>
		///A test for FromPacked44
		///</summary>
		[TestMethod()]
		public void FromPacked44Test()
		{
			var expected = new TeamPlayerId(0);
			var actual = TeamPlayerId.FromPacked44(0x0);
			Assert.AreEqual(expected, actual);

			expected = new TeamPlayerId(1);
			actual = TeamPlayerId.FromPacked44(0x10);
			Assert.AreEqual(expected, actual);

			expected = new TeamPlayerId(8);
			actual = TeamPlayerId.FromPacked44(0x17);
			Assert.AreEqual(expected, actual);

			expected = new TeamPlayerId(9);
			actual = TeamPlayerId.FromPacked44(0x20);
			Assert.AreEqual(expected, actual);

			expected = new TeamPlayerId(16);
			actual = TeamPlayerId.FromPacked44(0x27);
			Assert.AreEqual(expected, actual);

			expected = new TeamPlayerId(17);
			actual = TeamPlayerId.FromPacked44(0x30);
			Assert.AreEqual(expected, actual);

			expected = new TeamPlayerId(24);
			actual = TeamPlayerId.FromPacked44(0x37);
			Assert.AreEqual(expected, actual);
		}

		/// <summary>
		///A test for ToString
		///</summary>
		[TestMethod()]
		public void ToStringTest()
		{
			var target = new TeamPlayerId(0);
			Assert.AreEqual("0", target.ToString());

			target = new TeamPlayerId(1);
			Assert.AreEqual("1", target.ToString());

			target = new TeamPlayerId(8);
			Assert.AreEqual("8", target.ToString());

			target = new TeamPlayerId(9);
			Assert.AreEqual("9", target.ToString());

			target = new TeamPlayerId(16);
			Assert.AreEqual("16", target.ToString());

			target = new TeamPlayerId(17);
			Assert.AreEqual("17", target.ToString());

			target = new TeamPlayerId(24);
			Assert.AreEqual("24", target.ToString());
		}

		/// <summary>
		///A test for ToString
		///</summary>
		[TestMethod()]
		public void ToStringTest1()
		{
			var target = new TeamPlayerId(0);
			Assert.AreEqual("T0:P0", target.ToString(true));

			target = new TeamPlayerId(1);
			Assert.AreEqual("T1:P1", target.ToString(true));

			target = new TeamPlayerId(8);
			Assert.AreEqual("T1:P8", target.ToString(true));

			target = new TeamPlayerId(9);
			Assert.AreEqual("T2:P1", target.ToString(true));

			target = new TeamPlayerId(16);
			Assert.AreEqual("T2:P8", target.ToString(true));

			target = new TeamPlayerId(17);
			Assert.AreEqual("T3:P1", target.ToString(true));

			target = new TeamPlayerId(24);
			Assert.AreEqual("T3:P8", target.ToString(true));
		}
	}
}
