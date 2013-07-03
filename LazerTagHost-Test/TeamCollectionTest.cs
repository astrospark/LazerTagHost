using LazerTagHostLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LazerTagHost_Test
{
    
    
    /// <summary>
    ///This is a test class for TeamCollectionTest and is intended
    ///to contain all TeamCollectionTest Unit Tests
    ///</summary>
	[TestClass()]
	public class TeamCollectionTest
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
		///A test for CalculateRanks
		///</summary>
		[TestMethod()]
		public void CalculateRanksTest()
		{
			var team1 = new Team(1);
			var team2 = new Team(2);
			var team3 = new Team(3);
			var target = new TeamCollection {team1, team2, team3};

			team1.Score = 3;
			team2.Score = 2;
			team3.Score = 1;
			target.CalculateRanks();
			Assert.AreEqual(1, team1.Rank);
			Assert.AreEqual(2, team2.Rank);
			Assert.AreEqual(3, team3.Rank);

			team1.Score = 2;
			team2.Score = 3;
			team3.Score = 1;
			target.CalculateRanks();
			Assert.AreEqual(2, team1.Rank);
			Assert.AreEqual(1, team2.Rank);
			Assert.AreEqual(3, team3.Rank);

			team1.Score = 1;
			team2.Score = 2;
			team3.Score = 3;
			target.CalculateRanks();
			Assert.AreEqual(3, team1.Rank);
			Assert.AreEqual(2, team2.Rank);
			Assert.AreEqual(1, team3.Rank);

			team1.Score = 1;
			team2.Score = 0;
			team3.Score = -1;
			target.CalculateRanks();
			Assert.AreEqual(1, team1.Rank);
			Assert.AreEqual(2, team2.Rank);
			Assert.AreEqual(3, team3.Rank);

			team1.Score = 1;
			team2.Score = 1;
			team3.Score = 0;
			target.CalculateRanks();
			Assert.AreEqual(1, team1.Rank);
			Assert.AreEqual(1, team2.Rank);
			Assert.AreEqual(3, team3.Rank);

			team1.Score = 0;
			team2.Score = 0;
			team3.Score = 1;
			target.CalculateRanks();
			Assert.AreEqual(2, team1.Rank);
			Assert.AreEqual(2, team2.Rank);
			Assert.AreEqual(1, team3.Rank);
		}
	}
}
