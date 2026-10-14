using Godot;

public partial class AdminMapHUD : Control
{
	public override void _Ready()
	{
		Visible = UserSession.IsAdmin;
		if (!UserSession.IsAdmin)
			return;

		GetNode<Button>("TopLeft/HomeButton").Pressed += OnHomePressed;
		GetNode<Button>("TopLeft/QuestButton").Pressed += OnAddQuestPressed;
		GetNode<Button>("TopRight/ManageQuestButton").Pressed += OnAddQuestPressed;
		GetNode<Button>("TopRight/ManageNPCButton").Pressed += OnNpcSetupPressed;
		GetNode<Button>("TopRight/DeployNPCButton").Pressed += OnNpcSetupPressed;
	}

	private void OnHomePressed()
	{
		GetTree().ChangeSceneToFile(TechQuest.Core.DashboardNavigationPaths.AdminDashboard);
	}

	private void OnAddQuestPressed()
	{
		Node mapHudLayer = GetTree().CurrentScene?.GetNodeOrNull<Node>("MapHUDLayer");
		Control questPopup = mapHudLayer?.GetNodeOrNull<Control>("EasyMapQuest");
		if (questPopup == null)
			questPopup = mapHudLayer?.GetNodeOrNull<Control>("MediumMapQuest");
		if (questPopup == null)
			questPopup = mapHudLayer?.GetNodeOrNull<Control>("HardMapQuest");

		if (questPopup == null)
		{
			GD.PrintErr("Admin quest form was not found in the current map.");
			return;
		}

		questPopup.Call("OpenAdminQuestAdd");
	}

	private void OnNpcSetupPressed()
	{
		Node adminHud = GetTree().CurrentScene?.GetNodeOrNull<Node>("MapHUDLayer/AdminMapHUD");
		Control npcPopup = adminHud?.GetNodeOrNull<Control>("AdminEasyNpc");
		if (npcPopup == null)
			npcPopup = adminHud?.GetNodeOrNull<Control>("AdminMediumNpc");
		if (npcPopup == null)
			npcPopup = adminHud?.GetNodeOrNull<Control>("AdminHardNpc");

		if (npcPopup == null)
		{
			GD.PrintErr("Admin NPC setup was not found in the current map.");
			return;
		}

		npcPopup.Call("OpenNpcSetup");
	}
}
