﻿using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.ProjectAuditor.EditorTests
{
	class SettingIssueTests
	{	
		[Test]
		public void SettingIssuesAreReported()
		{
			var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

			UnityEditor.PlayerSettings.stripEngineCode = false;
			UnityEngine.Time.fixedDeltaTime = 0.02f; // default value
			var projectReport = projectAuditor.Audit();
			var issues = projectReport.GetIssues(IssueCategory.ProjectSettings);

			var fixedDeltaTimeIssue = issues.FirstOrDefault(i => i.descriptor.method.Equals("fixedDeltaTime"));
			Assert.NotNull(fixedDeltaTimeIssue);
			Assert.True(fixedDeltaTimeIssue.location.path.Equals("Project/Time"));
			
			UnityEngine.Time.fixedDeltaTime = 0.021f;
			
			projectReport = projectAuditor.Audit();
			issues = projectReport.GetIssues(IssueCategory.ProjectSettings);			
			Assert.Null(issues.FirstOrDefault(i => i.descriptor.method.Equals("fixedDeltaTime")));

			var playerSettingIssue =
				issues.FirstOrDefault(i => i.descriptor.method.Equals("stripEngineCode")); 
			Assert.NotNull(playerSettingIssue); 
			Assert.True(playerSettingIssue.location.path.Equals("Project/Player"));
		}
	}	
}

