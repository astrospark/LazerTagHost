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
			var target = new TeamPlayerId(1, 1);
			if (target.PlayerNumber != 1) Assert.Fail("TeamPlayerId(1, 1).PlayerNumber == {0} != 1", target.PlayerNumber);
			if (target.TeamNumber != 1) Assert.Fail("TeamPlayerId(1, 1).TeamNumber == {0} != 1", target.TeamNumber);
			if (target.TeamPlayerNumber != 1) Assert.Fail("TeamPlayerId(1, 1).TeamPlayerNumber == {0} != 1", target.TeamPlayerNumber);
			if (target.Packed23 != 0x8) Assert.Fail("TeamPlayerId(1, 1).Packed23 == {0} != 0x8 (00001000)", target.Packed23);
			if (target.Packed44 != 0x10) Assert.Fail("TeamPlayerId(1, 1).Packed44 == {0} != 0x10 (00010000)", target.Packed44);

			target = new TeamPlayerId(1, 8);
			if (target.PlayerNumber != 8) Assert.Fail("TeamPlayerId(1, 8).PlayerNumber == {0} != 8", target.PlayerNumber);
			if (target.TeamNumber != 1) Assert.Fail("TeamPlayerId(1, 8).TeamNumber == {0} != 1", target.TeamNumber);
			if (target.TeamPlayerNumber != 8) Assert.Fail("TeamPlayerId(1, 8).TeamPlayerNumber == {0} != 8", target.TeamPlayerNumber);
			if (target.Packed23 != 0xf) Assert.Fail("TeamPlayerId(1, 8).Packed23 == {0} != 00001111", target.Packed23);
			if (target.Packed44 != 0x17) Assert.Fail("TeamPlayerId(1, 8).Packed44 == {0} != 00010111", target.Packed44);

			target = new TeamPlayerId(2, 1);
			if (target.PlayerNumber != 9) Assert.Fail("TeamPlayerId(2, 1).PlayerNumber == {0} != 9", target.PlayerNumber);
			if (target.TeamNumber != 2) Assert.Fail("TeamPlayerId(2, 1).TeamNumber == {0} != 2", target.TeamNumber);
			if (target.TeamPlayerNumber != 1) Assert.Fail("TeamPlayerId(2, 1).TeamPlayerNumber == {0} != 1", target.TeamPlayerNumber);
			if (target.Packed23 != 0x10) Assert.Fail("TeamPlayerId(2, 1).Packed23 == {0} != 0x10 (00010000)", target.Packed23);
			if (target.Packed44 != 0x20) Assert.Fail("TeamPlayerId(2, 1).Packed44 == {0} != 0x20 (00100000)", target.Packed44);

			target = new TeamPlayerId(2, 8);
			if (target.PlayerNumber != 16) Assert.Fail("TeamPlayerId(2, 8).PlayerNumber == {0} != 16", target.PlayerNumber);
			if (target.TeamNumber != 2) Assert.Fail("TeamPlayerId(2, 8).TeamNumber == {0} != 2", target.TeamNumber);
			if (target.TeamPlayerNumber != 8) Assert.Fail("TeamPlayerId(2, 8).TeamPlayerNumber == {0} != 8", target.TeamPlayerNumber);
			if (target.Packed23 != 0x17) Assert.Fail("TeamPlayerId(2, 8).Packed23 == {0} != 0x17 (00010111)", target.Packed23);
			if (target.Packed44 != 0x27) Assert.Fail("TeamPlayerId(2, 8).Packed44 == {0} != 0x27 (00100111)", target.Packed44);

			target = new TeamPlayerId(3, 1);
			if (target.PlayerNumber != 17) Assert.Fail("TeamPlayerId(3, 1).PlayerNumber == {0} != 17", target.PlayerNumber);
			if (target.TeamNumber != 3) Assert.Fail("TeamPlayerId(3, 1).TeamNumber == {0} != 3", target.TeamNumber);
			if (target.TeamPlayerNumber != 1) Assert.Fail("TeamPlayerId(3, 1).TeamPlayerNumber == {0} != 1", target.TeamPlayerNumber);
			if (target.Packed23 != 0x18) Assert.Fail("TeamPlayerId(3, 1).Packed23 == {0} != 0x18 (00011000)", target.Packed23);
			if (target.Packed44 != 0x30) Assert.Fail("TeamPlayerId(3, 1).Packed44 == {0} != 0x30 (00110000)", target.Packed44);

			target = new TeamPlayerId(3, 8);
			if (target.PlayerNumber != 24) Assert.Fail("TeamPlayerId(3, 8).PlayerNumber == {0} != 24", target.PlayerNumber);
			if (target.TeamNumber != 3) Assert.Fail("TeamPlayerId(3, 8).TeamNumber == {0} != 3", target.TeamNumber);
			if (target.TeamPlayerNumber != 8) Assert.Fail("TeamPlayerId(3, 8).TeamPlayerNumber == {0} != 8", target.TeamPlayerNumber);
			if (target.Packed23 != 0x1f) Assert.Fail("TeamPlayerId(3, 8).Packed23 == {0} != 0x1f (00011111)", target.Packed23);
			if (target.Packed44 != 0x37) Assert.Fail("TeamPlayerId(3, 8).Packed44 == {0} != 0x37 (00110111)", target.Packed44);
		}

		/// <summary>
		///A test for TeamPlayerId Constructor
		///</summary>
		[TestMethod()]
		public void TeamPlayerIdConstructorTest1()
		{
			var target = new TeamPlayerId(1);
			if (target.PlayerNumber != 1) Assert.Fail("TeamPlayerId(1).PlayerNumber == {0} != 1", target.PlayerNumber);
			if (target.TeamNumber != 1) Assert.Fail("TeamPlayerId(1).TeamNumber == {0} != 1", target.TeamNumber);
			if (target.TeamPlayerNumber != 1) Assert.Fail("TeamPlayerId(1).TeamPlayerNumber == {0} != 1", target.TeamPlayerNumber);
			if (target.Packed23 != 0x8) Assert.Fail("TeamPlayerId(1).Packed23 == {0} != 0x8 (00001000)", target.Packed23);
			if (target.Packed44 != 0x10) Assert.Fail("TeamPlayerId(1).Packed44 == {0} != 0x10 (00010000)", target.Packed44);

			target = new TeamPlayerId(8);
			if (target.PlayerNumber != 8) Assert.Fail("TeamPlayerId(8).PlayerNumber == {0} != 8", target.PlayerNumber);
			if (target.TeamNumber != 1) Assert.Fail("TeamPlayerId(8).TeamNumber == {0} != 1", target.TeamNumber);
			if (target.TeamPlayerNumber != 8) Assert.Fail("TeamPlayerId(8).TeamPlayerNumber == {0} != 8", target.TeamPlayerNumber);
			if (target.Packed23 != 0xf) Assert.Fail("TeamPlayerId(8).Packed23 == {0} != 00001111", target.Packed23);
			if (target.Packed44 != 0x17) Assert.Fail("TeamPlayerId(8).Packed44 == {0} != 00010111", target.Packed44);

			target = new TeamPlayerId(9);
			if (target.PlayerNumber != 9) Assert.Fail("TeamPlayerId(9).PlayerNumber == {0} != 9", target.PlayerNumber);
			if (target.TeamNumber != 2) Assert.Fail("TeamPlayerId(9).TeamNumber == {0} != 2", target.TeamNumber);
			if (target.TeamPlayerNumber != 1) Assert.Fail("TeamPlayerId(9).TeamPlayerNumber == {0} != 1", target.TeamPlayerNumber);
			if (target.Packed23 != 0x10) Assert.Fail("TeamPlayerId(9).Packed23 == {0} != 0x10 (00010000)", target.Packed23);
			if (target.Packed44 != 0x20) Assert.Fail("TeamPlayerId(9).Packed44 == {0} != 0x20 (00100000)", target.Packed44);

			target = new TeamPlayerId(16);
			if (target.PlayerNumber != 16) Assert.Fail("TeamPlayerId(16).PlayerNumber == {0} != 16", target.PlayerNumber);
			if (target.TeamNumber != 2) Assert.Fail("TeamPlayerId(16).TeamNumber == {0} != 2", target.TeamNumber);
			if (target.TeamPlayerNumber != 8) Assert.Fail("TeamPlayerId(16).TeamPlayerNumber == {0} != 8", target.TeamPlayerNumber);
			if (target.Packed23 != 0x17) Assert.Fail("TeamPlayerId(16).Packed23 == {0} != 0x17 (00010111)", target.Packed23);
			if (target.Packed44 != 0x27) Assert.Fail("TeamPlayerId(16).Packed44 == {0} != 0x27 (00100111)", target.Packed44);

			target = new TeamPlayerId(17);
			if (target.PlayerNumber != 17) Assert.Fail("TeamPlayerId(17).PlayerNumber == {0} != 17", target.PlayerNumber);
			if (target.TeamNumber != 3) Assert.Fail("TeamPlayerId(17).TeamNumber == {0} != 3", target.TeamNumber);
			if (target.TeamPlayerNumber != 1) Assert.Fail("TeamPlayerId(17).TeamPlayerNumber == {0} != 1", target.TeamPlayerNumber);
			if (target.Packed23 != 0x18) Assert.Fail("TeamPlayerId(17).Packed23 == {0} != 0x18 (00011000)", target.Packed23);
			if (target.Packed44 != 0x30) Assert.Fail("TeamPlayerId(17).Packed44 == {0} != 0x30 (00110000)", target.Packed44);

			target = new TeamPlayerId(24);
			if (target.PlayerNumber != 24) Assert.Fail("TeamPlayerId(24).PlayerNumber == {0} != 24", target.PlayerNumber);
			if (target.TeamNumber != 3) Assert.Fail("TeamPlayerId(24).TeamNumber == {0} != 3", target.TeamNumber);
			if (target.TeamPlayerNumber != 8) Assert.Fail("TeamPlayerId(24).TeamPlayerNumber == {0} != 8", target.TeamPlayerNumber);
			if (target.Packed23 != 0x1f) Assert.Fail("TeamPlayerId(24).Packed23 == {0} != 0x1f (00011111)", target.Packed23);
			if (target.Packed44 != 0x37) Assert.Fail("TeamPlayerId(24).Packed44 == {0} != 0x37 (00110111)", target.Packed44);
		}

		/// <summary>
		///A test for Equals
		///</summary>
		[TestMethod()]
		public void EqualsTest()
		{
			TeamPlayerId target = new TeamPlayerId(); // TODO: Initialize to an appropriate value
			object obj = null; // TODO: Initialize to an appropriate value
			bool expected = false; // TODO: Initialize to an appropriate value
			bool actual;
			actual = target.Equals(obj);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for Equals
		///</summary>
		[TestMethod()]
		public void EqualsTest1()
		{
			TeamPlayerId target = new TeamPlayerId(); // TODO: Initialize to an appropriate value
			TeamPlayerId other = new TeamPlayerId(); // TODO: Initialize to an appropriate value
			bool expected = false; // TODO: Initialize to an appropriate value
			bool actual;
			actual = target.Equals(other);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for FromPacked23
		///</summary>
		[TestMethod()]
		public void FromPacked23Test()
		{
			var expected = new TeamPlayerId(1);
			var actual = TeamPlayerId.FromPacked23(0x8);
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
		///A test for FromPacked44
		///</summary>
		[TestMethod()]
		public void FromPacked44Test()
		{
			var expected = new TeamPlayerId(1);
			var actual = TeamPlayerId.FromPacked44(0x10);
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
		///A test for GetHashCode
		///</summary>
		[TestMethod()]
		public void GetHashCodeTest()
		{
			TeamPlayerId target = new TeamPlayerId(); // TODO: Initialize to an appropriate value
			int expected = 0; // TODO: Initialize to an appropriate value
			int actual;
			actual = target.GetHashCode();
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for ToString
		///</summary>
		[TestMethod()]
		public void ToStringTest()
		{
			var target = new TeamPlayerId(1);
			Assert.AreEqual(target.ToString(), "1");

			target = new TeamPlayerId(8);
			Assert.AreEqual(target.ToString(), "8");

			target = new TeamPlayerId(9);
			Assert.AreEqual(target.ToString(), "9");

			target = new TeamPlayerId(16);
			Assert.AreEqual(target.ToString(), "16");

			target = new TeamPlayerId(17);
			Assert.AreEqual(target.ToString(), "17");

			target = new TeamPlayerId(24);
			Assert.AreEqual(target.ToString(), "24");
		}

		/// <summary>
		///A test for ToString
		///</summary>
		[TestMethod()]
		public void ToStringTeamTest()
		{
			var target = new TeamPlayerId(1);
			Assert.AreEqual(target.ToString(true), "1:1");

			target = new TeamPlayerId(8);
			Assert.AreEqual(target.ToString(true), "1:8");

			target = new TeamPlayerId(9);
			Assert.AreEqual(target.ToString(true), "2:1");

			target = new TeamPlayerId(16);
			Assert.AreEqual(target.ToString(true), "2:8");

			target = new TeamPlayerId(17);
			Assert.AreEqual(target.ToString(true), "3:1");

			target = new TeamPlayerId(24);
			Assert.AreEqual(target.ToString(true), "3:8");
		}

		/// <summary>
		///A test for op_Equality
		///</summary>
		[TestMethod()]
		public void op_EqualityTest()
		{
			TeamPlayerId first = new TeamPlayerId(); // TODO: Initialize to an appropriate value
			TeamPlayerId second = new TeamPlayerId(); // TODO: Initialize to an appropriate value
			bool expected = false; // TODO: Initialize to an appropriate value
			bool actual;
			actual = (first == second);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for op_Inequality
		///</summary>
		[TestMethod()]
		public void op_InequalityTest()
		{
			TeamPlayerId first = new TeamPlayerId(); // TODO: Initialize to an appropriate value
			TeamPlayerId second = new TeamPlayerId(); // TODO: Initialize to an appropriate value
			bool expected = false; // TODO: Initialize to an appropriate value
			bool actual;
			actual = (first != second);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for Packed23
		///</summary>
		[TestMethod()]
		public void Packed23Test()
		{
			TeamPlayerId target = new TeamPlayerId(); // TODO: Initialize to an appropriate value
			ushort expected = 0; // TODO: Initialize to an appropriate value
			ushort actual;
			target.Packed23 = expected;
			actual = target.Packed23;
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for Packed44
		///</summary>
		[TestMethod()]
		public void Packed44Test()
		{
			TeamPlayerId target = new TeamPlayerId(); // TODO: Initialize to an appropriate value
			ushort expected = 0; // TODO: Initialize to an appropriate value
			ushort actual;
			target.Packed44 = expected;
			actual = target.Packed44;
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for PlayerNumber
		///</summary>
		[TestMethod()]
		public void PlayerNumberTest()
		{
			TeamPlayerId target = new TeamPlayerId(); // TODO: Initialize to an appropriate value
			int expected = 0; // TODO: Initialize to an appropriate value
			int actual;
			target.PlayerNumber = expected;
			actual = target.PlayerNumber;
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for TeamNumber
		///</summary>
		[TestMethod()]
		public void TeamNumberTest()
		{
			TeamPlayerId target = new TeamPlayerId(); // TODO: Initialize to an appropriate value
			int expected = 0; // TODO: Initialize to an appropriate value
			int actual;
			target.TeamNumber = expected;
			actual = target.TeamNumber;
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for TeamPlayerNumber
		///</summary>
		[TestMethod()]
		public void TeamPlayerNumberTest()
		{
			TeamPlayerId target = new TeamPlayerId(); // TODO: Initialize to an appropriate value
			int expected = 0; // TODO: Initialize to an appropriate value
			int actual;
			target.TeamPlayerNumber = expected;
			actual = target.TeamPlayerNumber;
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}
	}
}
