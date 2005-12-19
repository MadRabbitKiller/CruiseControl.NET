using System;
using Exortech.NetReflector;
using ThoughtWorks.CruiseControl.Core.Util;
using ThoughtWorks.CruiseControl.Remote;

namespace ThoughtWorks.CruiseControl.Core.Triggers
{
	[ReflectorType("projectTrigger")]
	public class ProjectTrigger : ITrigger
	{
		public const string DefaultServerUri = RemoteCruiseServer.DefaultUri;
		private const int DefaultIntervalSeconds = 5;

		private readonly ICruiseManagerFactory managerFactory;
		private ProjectStatus lastStatus;

		public ProjectTrigger() : this(new RemoteCruiseManagerFactory())
		{}

		public ProjectTrigger(ICruiseManagerFactory managerFactory)
		{
			this.managerFactory = managerFactory;
		}

		[ReflectorProperty("project")]
		public string Project;

		[ReflectorProperty("serverUri", Required=false)]
		public string ServerUri = DefaultServerUri;

		[ReflectorProperty("triggerStatus", Required=false)]
		public IntegrationStatus TriggerStatus = IntegrationStatus.Success;

		[ReflectorProperty("innerTrigger", InstanceTypeKey="type", Required=false)]
		public ITrigger InnerTrigger = NewIntervalTrigger();

		public BuildCondition ShouldRunIntegration()
		{
			BuildCondition buildCondition = InnerTrigger.ShouldRunIntegration();
			if (buildCondition == BuildCondition.NoBuild) return buildCondition;
			IntegrationCompleted();	// reset inner trigger

			ProjectStatus currentStatus = GetCurrentProjectStatus();
			if (lastStatus == null)
			{
				lastStatus = currentStatus;
				return BuildCondition.NoBuild;
			}
			if (currentStatus.LastBuildDate > lastStatus.LastBuildDate && currentStatus.BuildStatus == TriggerStatus)
			{
				lastStatus = currentStatus;
				return buildCondition;
			}
			return BuildCondition.NoBuild;
		}

		public void IntegrationCompleted()
		{
			InnerTrigger.IntegrationCompleted();
		}

		private ProjectStatus GetCurrentProjectStatus()
		{
			Log.Debug("Retrieving ProjectStatus from server: " + ServerUri);
			ProjectStatus[] currentStatuses = managerFactory.GetCruiseManager(ServerUri).GetProjectStatus();
			foreach (ProjectStatus currentStatus in currentStatuses)
			{
				if (currentStatus.Name == Project)
				{
					return currentStatus;
				}
			}
			throw new NoSuchProjectException(Project);
		}

		public DateTime NextBuild
		{
			get
			{
				if (lastStatus == null) return InnerTrigger.NextBuild;
				return lastStatus.NextBuildTime;
			}
		}

		private static ITrigger NewIntervalTrigger()
		{
			IntervalTrigger trigger = new IntervalTrigger();
			trigger.IntervalSeconds = DefaultIntervalSeconds;
			trigger.BuildCondition = BuildCondition.ForceBuild;
			return trigger;
		}
	}
}