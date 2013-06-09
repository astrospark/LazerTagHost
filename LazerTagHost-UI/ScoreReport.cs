using System;
using System.IO;
using System.Windows.Forms;
using DotLiquid;
using LazerTagHostLibrary;

namespace LazerTagHostUI
{
	public partial class ScoreReport : Form
	{
		public ScoreReport()
		{
			InitializeComponent();
		}

		public ScoreReport(HostGun hostGun)
		{
			InitializeComponent();
			_hostGun = hostGun;
		}

		private readonly HostGun _hostGun;

		private void ScoreReport_Load(object sender, EventArgs e)
		{
			try
			{
				var contentTemplate = File.ReadAllText(@"html\ScoreReport.html");
				var template = Template.Parse(contentTemplate);
				Template.RegisterFilter(typeof (OrdinalFilter));
				Template.RegisterFilter(typeof (YesNoFilter));
				Template.RegisterSafeType(typeof (Team), new[] {"Number", "Rank"});
				Template.RegisterSafeType(typeof (Player),
				                          new[] {"TeamPlayerId", "DisplayName", "Rank", "Score", "Survived", "TaggedByPlayerCounts"});
				Template.RegisterSafeType(typeof (TeamPlayerId), new[] {"PlayerNumber", "TeamNumber", "TeamPlayerNumber"});

				var executableDirectory = Path.GetDirectoryName(Application.ExecutablePath);
				if (executableDirectory == null)
					throw new Exception("Could not determine the name of the directory in which this executable is located.");
				var basePath = Path.Combine(executableDirectory, "html");

				var content = template.Render(Hash.FromAnonymousObject(new
					{
						base_uri = basePath,
						is_team_game = _hostGun.GameDefinition.IsTeamGame,
						teams = _hostGun.Teams,
						players = _hostGun.Players,
					}));

				webBrowser.DocumentText = content;
				textBox.Text = content;
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, String.Format("Unable to show Score Report.\n\n{0}", ex));
				Close();
			}
		}

		public static class YesNoFilter 
		{
			public static string YesNo(bool value)
			{
				return value ? "Yes" : "No";
			}
		}

		private void viewPageSourceToolStripMenuItem_Click(object sender, EventArgs e)
		{
			viewPageSourceToolStripMenuItem.Checked = !viewPageSourceToolStripMenuItem.Checked;
			splitContainer.Panel2Collapsed = !viewPageSourceToolStripMenuItem.Checked;
		}
	}
}
