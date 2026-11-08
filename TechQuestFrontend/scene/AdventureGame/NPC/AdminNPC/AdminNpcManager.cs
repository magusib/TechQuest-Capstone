using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public abstract partial class AdminNpcManager : Control
{
	protected abstract string Difficulty { get; }

	private GridContainer itemContainer;
	private Control itemTemplate;
	private Control addPopup;
	private LineEdit npcNameInput;
	private Button imageButton;
	private OptionButton animationSceneInput;
	private Button saveButton;
	private Label formMessage;
	private AcceptDialog messageDialog;
	private ConfirmationDialog deleteDialog;
	private ConfirmationDialog deployDialog;
	private ConfirmationDialog undeployDialog;
	private FileDialog fileDialog;
	private string selectedImagePath = "";
	private string selectedAnimationScenePath = "";
	private readonly List<string> animationSceneChoices = new();
	private NpcDto editingNpc;
	private NpcDto selectedNpc;
	private NpcDeploymentDto selectedDeployment;
	private readonly List<NpcDto> currentNpcs = new();
	private readonly List<NpcDeploymentDto> currentDeployments = new();
	private string npcPanelPath;
	private bool showingDeployedNpcs;
	private bool isLoadingNpcs;
	private bool isSavingNpc;
	private bool isDeletingNpc;
	private bool isDeployingNpc;
	private bool isUndeployingNpc;
	private bool isSavingConfiguration;
	private Control configurationPopup;
	private OptionButton subjectInput;
	private LineEdit questionInput;
	private LineEdit correctAnswerInput;
	private LineEdit answerTipInput;
	private LineEdit timerInput;
	private Label configurationTitle;
	private OptionButton miniGameInput;
	private Button saveConfigurationButton;
	private NpcConfigurationDto editingConfiguration;

	public override void _Ready()
	{
		Hide();
		npcPanelPath = Difficulty + "NPCPanel";
		itemContainer = GetNode<GridContainer>(npcPanelPath + "/NPCListPanel/ScrollContainer/ItemContainer");
		itemTemplate = GetNode<Control>(npcPanelPath + "/NPCListPanel/NPCItemPanel");
		itemTemplate.Hide();
		addPopup = GetNode<Control>(npcPanelPath + "/AddNPCPopup");
		addPopup.Hide();
		npcNameInput = GetNode<LineEdit>(npcPanelPath + "/AddNPCPopup/AddItemPopup/MarginContainer/VBoxContainer/ItemName");
		imageButton = GetNode<Button>(npcPanelPath + "/AddNPCPopup/AddItemPopup/MarginContainer/VBoxContainer/ImageButton");
		saveButton = GetNode<Button>(npcPanelPath + "/AddNPCPopup/AddItemPopup/MarginContainer/VBoxContainer/Buttons/AddButton");
		formMessage = GetNode<Label>(npcPanelPath + "/AddNPCPopup/AddItemPopup/MarginContainer/VBoxContainer/Title");
		AddAnimationScenePicker();
		messageDialog = GetNode<AcceptDialog>(npcPanelPath + "/NPCMessageDialog/AcceptDialog");
		deleteDialog = GetNode<ConfirmationDialog>(npcPanelPath + "/NPCMessageDialog/DeleteConfirmationDialog");
		deployDialog = GetNode<ConfirmationDialog>(npcPanelPath + "/NPCMessageDialog/DeployConfirmationDialog");
		undeployDialog = GetNode<ConfirmationDialog>(npcPanelPath + "/NPCMessageDialog/UndeployConfirmationDialog");
		CreateConfigurationPopup();

		GetNode<Button>(npcPanelPath + "/AddButton").Pressed += OpenAddForm;
		GetNode<Button>(npcPanelPath + "/CloseButton").Pressed += CloseSetup;
		GetNode<Button>(npcPanelPath + "/AddNPCPopup/AddItemPopup/MarginContainer/VBoxContainer/Buttons/CancelButton").Pressed += CloseForm;
		imageButton.Pressed += OpenImagePicker;
		saveButton.Pressed += SaveNpc;
		deleteDialog.Confirmed += DeleteSelectedNpc;
		deployDialog.Confirmed += DeploySelectedNpc;
		undeployDialog.Confirmed += UndeploySelectedNpc;

		if (OS.GetName() != "Android")
		{
			fileDialog = new FileDialog
			{
				Title = "Choose Local NPC Image Asset",
				FileMode = FileDialog.FileModeEnum.OpenFile,
				Access = FileDialog.AccessEnum.Resources,
				Filters = new string[] { "*.png ; PNG Images", "*.jpg ; JPG Images", "*.jpeg ; JPEG Images" }
			};
			AddChild(fileDialog);
			fileDialog.FileSelected += OnImageSelected;
		}

	}

	private void AddAnimationScenePicker()
	{
		var fields = (VBoxContainer)imageButton.GetParent();
		int insertIndex = imageButton.GetIndex() + 1;

		var animationLabel = new Label
		{
			Text = "Animation scene (optional)",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		fields.AddChild(animationLabel);
		fields.MoveChild(animationLabel, insertIndex);

		animationSceneInput = new OptionButton
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		animationSceneInput.ItemSelected += OnAnimationSceneSelected;
		fields.AddChild(animationSceneInput);
		fields.MoveChild(animationSceneInput, insertIndex + 1);
		PopulateAnimationSceneChoices();
	}

	private void PopulateAnimationSceneChoices()
	{
		if (animationSceneInput == null)
			return;

		animationSceneChoices.Clear();
		animationSceneInput.Clear();
		animationSceneInput.AddItem("No animation (use local image)");
		animationSceneChoices.Add("");

		string folder = $"res://scene/AdventureGame/NPC/{Difficulty}MapNPC";
		string scenePrefix = $"{folder}/{Difficulty}MapNPC";
		for (int index = 0; index <= 5; index++)
		{
			string path = index == 0
				? $"{scenePrefix}.tscn"
				: $"{scenePrefix}{index}.tscn";
			if (!ResourceLoader.Exists(path))
				continue;

			animationSceneInput.AddItem(index == 0 ? "Default NPC" : $"NPC {index}");
			animationSceneChoices.Add(path);
		}
	}

	public void OpenNpcSetup()
	{
		OpenUndeployedNpcList();
	}

	public void OpenDeployedNpcList()
	{
		OpenNpcList(true);
	}

	public void OpenUndeployedNpcList()
	{
		OpenNpcList(false);
	}

	private async void OpenNpcList(bool showDeployed)
	{
		if (!UserSession.IsAdmin)
			return;

		showingDeployedNpcs = showDeployed;
		Show();
		GetNode<Control>(npcPanelPath).Show();
		addPopup.Hide();
		configurationPopup.Hide();
		GetNode<Label>(npcPanelPath + "/" + Difficulty + "NPCLabel").Text = showDeployed
			? "Manage Deployed NPCs"
			: "NPC Inventory";
		GetNode<Button>(npcPanelPath + "/AddButton").Visible = !showDeployed;
		await LoadNpcsAsync();
	}

	private async Task LoadNpcsAsync()
	{
		if (isLoadingNpcs)
			return;

		isLoadingNpcs = true;
		try
		{
			currentNpcs.Clear();
			currentNpcs.AddRange(await NpcApiClient.GetNpcsAsync(Difficulty));
			currentDeployments.Clear();
			currentDeployments.AddRange(await NpcApiClient.GetDeploymentsAsync(Difficulty));
			foreach (Node child in itemContainer.GetChildren())
				child.QueueFree();

			if (!showingDeployedNpcs)
			{
				foreach (NpcDto npc in currentNpcs)
					await AddNpcCardAsync(npc, null);
			}

			if (showingDeployedNpcs)
			{
				foreach (NpcDeploymentDto deployment in currentDeployments)
				{
					NpcDto npc = FindNpc(deployment.NpcId);
					if (npc != null)
						await AddNpcCardAsync(npc, deployment);
				}
			}
		}
		catch (Exception exception)
		{
			ShowMessage("NPC list could not be loaded: " + exception.Message);
		}
		finally
		{
			isLoadingNpcs = false;
		}
	}

	private async Task AddNpcCardAsync(NpcDto npc, NpcDeploymentDto deployment)
	{
		var card = new PanelContainer
		{
			CustomMinimumSize = new Vector2(190, 300),
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin
		};
		var layout = new VBoxContainer();
		layout.AddThemeConstantOverride("separation", 8);
		card.AddChild(layout);

		var image = new TextureRect
		{
			CustomMinimumSize = new Vector2(165, 185),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
		};
		layout.AddChild(image);
		var label = new Label
		{
			Text = npc.NpcName,
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		layout.AddChild(label);

		if (deployment != null)
		{
			var statusLabel = new Label
			{
				Text = deployment.Status == "Pending"
					? "Pending - Waiting for an available spawn point."
					: "Deployed",
				HorizontalAlignment = HorizontalAlignment.Center,
				AutowrapMode = TextServer.AutowrapMode.WordSmart
			};
			layout.AddChild(statusLabel);
			var actionRow = new HBoxContainer();
			layout.AddChild(actionRow);
			var manageButton = new Button
			{
				Text = "Manage",
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			var undeployButton = new Button
			{
				Text = deployment.Status == "Pending" ? "Cancel Pending" : "Undeploy",
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			actionRow.AddChild(manageButton);
			actionRow.AddChild(undeployButton);
			manageButton.Pressed += () => OpenConfigurationAsync(npc, deployment);
			undeployButton.Pressed += () => ConfirmUndeploy(npc, deployment);
			if (deployment.Status == "Pending")
				manageButton.Text = "Set Data";
		}
		else
		{
			var deployButton = new Button { Text = "Deploy" };
			layout.AddChild(deployButton);
			var editDeleteRow = new HBoxContainer();
			layout.AddChild(editDeleteRow);
			var editButton = new Button { Text = "Edit", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
			var deleteButton = new Button { Text = "Delete", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
			editDeleteRow.AddChild(editButton);
			editDeleteRow.AddChild(deleteButton);
			deployButton.Pressed += () => ConfirmDeploy(npc);
			editButton.Pressed += () => OpenEditForm(npc);
			deleteButton.Pressed += () => ConfirmDelete(npc);
		}
		itemContainer.AddChild(card);

		if (!string.IsNullOrWhiteSpace(npc.NpcImage))
		{
			try
			{
				image.Texture = await AccountApiClient.LoadAvatarAsync(npc.NpcImage);
			}
			catch (Exception exception)
			{
				GD.PrintErr("NPC image could not be loaded: " + exception.Message);
			}
		}
	}

	private NpcDto FindNpc(int npcId)
	{
		foreach (NpcDto npc in currentNpcs)
		{
			if (npc.NpcId == npcId)
				return npc;
		}

		return null;
	}

	private void CreateConfigurationPopup()
	{
		configurationPopup = GetNode<Control>(Difficulty + "MapNPCManagement");
		configurationPopup.Hide();
		configurationTitle = configurationPopup.GetNode<Label>("NPCManagementLabel");
		subjectInput = configurationPopup.GetNode<OptionButton>("SubjectLabel/Subject");
		miniGameInput = configurationPopup.GetNode<OptionButton>("MinigameLabel/MinigameOptions");
		questionInput = configurationPopup.GetNode<LineEdit>("QuestionLabel/Question");
		configurationPopup.GetNode<Control>("AnswerChoicesLabel").Hide();
		correctAnswerInput = configurationPopup.GetNode<LineEdit>("CorrectAnswerLabel/CorrectAnswer");
		answerTipInput = configurationPopup.GetNode<LineEdit>("AnswerTipLabel/AnswerTip");
		timerInput = configurationPopup.GetNode<LineEdit>("TimerLabel/Time");
		saveConfigurationButton = configurationPopup.GetNode<Button>("SaveButton");
		Button cancelButton = configurationPopup.GetNode<Button>("CancelButton");
		cancelButton.Pressed += () => configurationPopup.Hide();
		saveConfigurationButton.Pressed += SaveConfigurationAsync;
	}

	private async void OpenConfigurationAsync(NpcDto npc, NpcDeploymentDto deployment)
	{
		try
		{
			NpcConfigurationDto configuration = await NpcApiClient.GetDeploymentConfigurationAsync(
				deployment.DeploymentId);
			List<MiniGameDto> miniGames = await NpcApiClient.GetMiniGamesAsync();
			if (!IsInsideTree())
				return;

			editingConfiguration = configuration;
			configurationTitle.Text = "Manage " + npc.NpcName + " (Deployment " +
				deployment.DeploymentId + ")";
			SelectSubject(configuration.SubjectName);
			questionInput.Text = configuration.Question;
			correctAnswerInput.Text = configuration.CorrectAnswer;
			answerTipInput.Text = configuration.AnswerTip;
			timerInput.Text = configuration.TimerSeconds.ToString();
			miniGameInput.Clear();
			miniGameInput.AddItem("No mini-game", 0);
			foreach (MiniGameDto miniGame in miniGames)
			{
				if (IsSupportedMiniGameType(miniGame.GameType))
					miniGameInput.AddItem(miniGame.GameName, miniGame.MiniGameId);
			}

			miniGameInput.Select(0);
			if (configuration.MiniGameId.HasValue)
			{
				for (int index = 1; index < miniGameInput.ItemCount; index++)
				{
					if (miniGameInput.GetItemId(index) == configuration.MiniGameId.Value)
					{
						miniGameInput.Select(index);
						break;
					}
				}
			}

			configurationPopup.Show();
		}
		catch (Exception exception)
		{
			ShowMessage("NPC information could not be loaded: " + exception.Message);
		}
	}

	private static bool IsSupportedMiniGameType(string gameType)
	{
		return gameType == "MatchThePairs" ||
			gameType == "FallingDuck" ||
			gameType == "QuickTapChallenge";
	}

	private void SelectSubject(string selectedSubject)
	{
		subjectInput.Clear();
		subjectInput.AddItem("Select a subject");

		string[] subjects = Difficulty switch
		{
			"Easy" =>
			[
				"GECUTS - Understanding the Self",
				"GECMMW - Math in the Modern World",
				"FIL 1 - Komunikasyon sa Akademikong Filipino",
				"NSTP 1 - Civic Welfare & Training Services 1",
				"PATHFIT 1 - Physical Activities Toward Health and Fitness 1",
				"GECARTA - Art Appreciation",
				"GECSTS - Science, Technology and Society",
				"FIL 2 - Pagbasa at Pagsulat tungo sa Pananaliksik",
				"NSTP 2 - Civic Welfare & Training Services 2",
				"PATHFIT 2 - Advance Physical Fitness",
				"GECPC - Purposive Communication"
			],
			"Medium" =>
			[
				"GECTCW - The Contemporary world",
				"GECETH - Professional Ethics",
				"GEEMST - Living in IT Era",
				"PATHFIT 3 - Teamsports",
				"PATHFIT 4 - Dance",
				"GEEAH - Reading in Visual Arts"
			],
			"Hard" =>
			[
				"GEESSP - The Entrepreneurial Mind",
				"GEMRZL - Rizal Life Works and Writing",
				"SP 101 - Social Issues and Professional Practices"
			],
			_ => throw new InvalidOperationException($"Unsupported NPC difficulty: {Difficulty}")
		};

		int selectedIndex = -1;
		foreach (string subject in subjects)
		{
			subjectInput.AddItem(subject);
			if (string.Equals(subject, selectedSubject, StringComparison.OrdinalIgnoreCase))
				selectedIndex = subjectInput.ItemCount - 1;
		}

		if (selectedIndex < 0 && !string.IsNullOrWhiteSpace(selectedSubject))
		{
			subjectInput.AddItem(selectedSubject);
			selectedIndex = subjectInput.ItemCount - 1;
		}

		subjectInput.Select(Math.Max(0, selectedIndex));
	}

	private async void SaveConfigurationAsync()
	{
		if (editingConfiguration == null || isSavingConfiguration)
			return;

		if (subjectInput.Selected <= 0 ||
			string.IsNullOrWhiteSpace(questionInput.Text) ||
			string.IsNullOrWhiteSpace(correctAnswerInput.Text))
		{
			ShowMessage("Subject, question, and correct answer are required.");
			return;
		}

		if (!int.TryParse(timerInput.Text, out int timerSeconds) || timerSeconds < 1)
		{
			ShowMessage("Timer must be a positive number of seconds.");
			return;
		}

		isSavingConfiguration = true;
		saveConfigurationButton.Disabled = true;
		editingConfiguration.SubjectName = subjectInput.GetItemText(subjectInput.Selected);
		int miniGameId = miniGameInput.GetItemId(miniGameInput.Selected);
		editingConfiguration.MiniGameId = miniGameId > 0 ? miniGameId : null;
		editingConfiguration.Question = questionInput.Text.Trim();
		editingConfiguration.AnswerChoices = new List<string>();
		editingConfiguration.CorrectAnswer = correctAnswerInput.Text.Trim();
		editingConfiguration.AnswerTip = answerTipInput.Text.Trim();
		editingConfiguration.TimerSeconds = timerSeconds;
		try
		{
			await NpcApiClient.SaveDeploymentConfigurationAsync(
				editingConfiguration.DeploymentId ?? 0,
				editingConfiguration);
			configurationPopup.Hide();
			ShowMessage("NPC information saved successfully.");
		}
		catch (Exception exception)
		{
			ShowMessage("NPC information could not be saved: " + exception.Message);
		}
		finally
		{
			isSavingConfiguration = false;
			saveConfigurationButton.Disabled = false;
		}
	}

	private void ConfirmUndeploy(NpcDto npc, NpcDeploymentDto deployment)
	{
		selectedNpc = npc;
		selectedDeployment = deployment;
		undeployDialog.DialogText = deployment.Status == "Pending"
			? $"Cancel the pending deployment of {npc.NpcName}?"
			: $"Undeploy {npc.NpcName} from the {Difficulty} map?";
		undeployDialog.PopupCentered();
	}

	private async void UndeploySelectedNpc()
	{
		if (selectedNpc == null || selectedDeployment == null || isUndeployingNpc)
			return;

		isUndeployingNpc = true;
		NpcDto npcToUndeploy = selectedNpc;
		NpcDeploymentDto deploymentToRemove = selectedDeployment;
		try
		{
			await NpcApiClient.UndeployNpcAsync(deploymentToRemove.DeploymentId);
			RemoveDeploymentSprite(deploymentToRemove.DeploymentId);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await RefreshMapDeploymentsAsync();
			await LoadNpcsAsync();
			ShowMessage(deploymentToRemove.Status == "Pending"
				? $"Pending deployment for {npcToUndeploy.NpcName} was cancelled."
				: $"{npcToUndeploy.NpcName} was undeployed.");
		}
		catch (Exception exception)
		{
			ShowMessage("NPC could not be undeployed: " + exception.Message);
		}
		finally
		{
			selectedNpc = null;
			selectedDeployment = null;
			isUndeployingNpc = false;
		}
	}

	private void OpenAddForm()
	{
		editingNpc = null;
		selectedImagePath = "";
		SelectAnimationScene("");
		npcNameInput.Text = "";
		imageButton.Text = "SELECT LOCAL IMAGE (OPTIONAL)";
		saveButton.Text = "SAVE";
		formMessage.Text = "Add NPC";
		addPopup.Show();
	}

	private void OpenEditForm(NpcDto npc)
	{
		editingNpc = npc;
		selectedImagePath = "";
		SelectAnimationScene(npc.AnimationScenePath ?? "");
		npcNameInput.Text = npc.NpcName;
		imageButton.Text = "SELECT LOCAL IMAGE (OPTIONAL)";
		saveButton.Text = "SAVE";
		formMessage.Text = "Edit NPC";
		addPopup.Show();
	}

	private void OpenImagePicker()
	{
		if (fileDialog != null)
			fileDialog.PopupCentered(new Vector2I(720, 520));
	}

	private void OnAnimationSceneSelected(long index)
	{
		int choiceIndex = (int)index;
		selectedAnimationScenePath = choiceIndex >= 0 && choiceIndex < animationSceneChoices.Count
			? animationSceneChoices[choiceIndex]
			: "";
	}

	private void SelectAnimationScene(string path)
	{
		selectedAnimationScenePath = "";
		if (animationSceneInput == null)
			return;

		for (int index = 0; index < animationSceneChoices.Count; index++)
		{
			if (animationSceneChoices[index] == path)
			{
				animationSceneInput.Select(index);
				selectedAnimationScenePath = path;
				return;
			}
		}

		animationSceneInput.Select(0);
	}

	private void OnImageSelected(string path)
	{
		const string expectedPrefix = "res://Items/NPC/";
		string extension = Path.GetExtension(path).ToLowerInvariant();
		if (!path.StartsWith(expectedPrefix, StringComparison.Ordinal) ||
			(extension != ".png" && extension != ".jpg" && extension != ".jpeg"))
		{
			ShowMessage("Choose a PNG or JPEG image from res://Items/NPC.");
			return;
		}

		selectedImagePath = path;
		imageButton.Text = Path.GetFileName(path);
	}

	private async void SaveNpc()
	{
		if (isSavingNpc)
			return;

		string name = npcNameInput.Text.Trim();
		if (string.IsNullOrWhiteSpace(name))
		{
			ShowMessage("NPC name is required.");
			return;
		}
		if (editingNpc == null &&
			string.IsNullOrWhiteSpace(selectedImagePath) &&
			string.IsNullOrWhiteSpace(selectedAnimationScenePath))
		{
			ShowMessage("Choose a local NPC image or an animation scene.");
			return;
		}

		isSavingNpc = true;
		saveButton.Disabled = true;
		try
		{
			string imagePath = editingNpc?.NpcImage ?? "";
			if (!string.IsNullOrWhiteSpace(selectedImagePath))
				imagePath = selectedImagePath;
			else if (!string.IsNullOrWhiteSpace(selectedAnimationScenePath))
				imagePath = "";

			var npc = new NpcDto
			{
				NpcId = editingNpc?.NpcId ?? 0,
				NpcName = name,
				NpcImage = imagePath,
				AnimationScenePath = selectedAnimationScenePath,
				Difficulty = Difficulty,
				Status = "active"
			};
			await NpcApiClient.SaveNpcAsync(npc);
			addPopup.Hide();
			await LoadNpcsAsync();
			ShowMessage("NPC saved successfully.");
		}
		catch (Exception exception)
		{
			ShowMessage("NPC could not be saved: " + exception.Message);
		}
		finally
		{
			isSavingNpc = false;
			saveButton.Disabled = false;
		}
	}

	private void ConfirmDelete(NpcDto npc)
	{
		selectedNpc = npc;
		deleteDialog.DialogText = $"Delete {npc.NpcName}?";
		deleteDialog.PopupCentered();
	}

	private async void DeleteSelectedNpc()
	{
		if (selectedNpc == null || isDeletingNpc)
			return;

		isDeletingNpc = true;
		NpcDto npcToDelete = selectedNpc;
		try
		{
			await NpcApiClient.DeleteNpcAsync(npcToDelete.NpcId);
			RemoveNpcSprites(npcToDelete.NpcId);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await RefreshMapDeploymentsAsync();
			selectedNpc = null;
			await LoadNpcsAsync();
			ShowMessage("NPC deleted successfully.");
		}
		catch (Exception exception)
		{
			ShowMessage("NPC could not be deleted: " + exception.Message);
		}
		finally
		{
			isDeletingNpc = false;
		}
	}

	private void ConfirmDeploy(NpcDto npc)
	{
		selectedNpc = npc;
		deployDialog.DialogText = $"Deploy {npc.NpcName} to the {Difficulty} map?";
		deployDialog.PopupCentered();
	}

	private async void DeploySelectedNpc()
	{
		if (selectedNpc == null || isDeployingNpc)
			return;

		isDeployingNpc = true;
		NpcDto npcToDeploy = selectedNpc;
		try
		{
			Node currentScene = GetTree().CurrentScene;
			Node spawnPoints = currentScene?.GetNodeOrNull<Node>("NPCSpawnPoints");
			if (spawnPoints == null)
				throw new InvalidOperationException("This map has no NPCSpawnPoints node.");

			var markers = new List<Marker2D>();
			foreach (Node child in spawnPoints.GetChildren())
			{
				if (child is Marker2D marker)
					markers.Add(marker);
			}

			List<NpcDeploymentDto> activeDeployments = await NpcApiClient.GetDeploymentsAsync(Difficulty);
			var occupiedMarkers = new HashSet<string>(StringComparer.Ordinal);
			foreach (NpcDeploymentDto activeDeployment in activeDeployments)
			{
				if (activeDeployment.Status == "Deployed" &&
					!string.IsNullOrWhiteSpace(activeDeployment.PositionMarker))
					occupiedMarkers.Add(activeDeployment.PositionMarker);
			}
			foreach (Node child in currentScene.GetChildren())
			{
				if (child is AnimatedSprite2D sprite &&
					!sprite.IsQueuedForDeletion() &&
					sprite.HasMeta("PositionMarker"))
					occupiedMarkers.Add(sprite.GetMeta("PositionMarker").AsString());
			}

			var availableMarkers = new List<Marker2D>();
			foreach (Marker2D marker in markers)
			{
				if (!occupiedMarkers.Contains(marker.Name.ToString()))
					availableMarkers.Add(marker);
			}

			Marker2D markerChoice = availableMarkers.Count > 0
				? availableMarkers[GD.RandRange(0, availableMarkers.Count - 1)]
				: null;
			Vector2? position = markerChoice == null ? null : markerChoice.GlobalPosition;

			NpcDeploymentDto deployment = await NpcApiClient.DeployNpcAsync(
				npcToDeploy,
				Difficulty,
				markerChoice?.Name,
				position);
			await RefreshMapDeploymentsAsync();
			List<NpcDeploymentDto> updatedDeployments = await NpcApiClient.GetDeploymentsAsync(Difficulty);
			NpcDeploymentDto updatedDeployment = updatedDeployments.Find(
				item => item.DeploymentId == deployment.DeploymentId) ?? deployment;
			await LoadNpcsAsync();
			ShowMessage(updatedDeployment.Status == "Pending"
				? "No available spawn point. The NPC deployment has been saved and will be deployed automatically when a spawn point becomes available."
				: "NPC deployed successfully.");
		}
		catch (Exception exception)
		{
			ShowMessage("NPC deployment failed: " + exception.Message);
		}
		finally
		{
			isDeployingNpc = false;
		}
	}

	private async Task RefreshMapDeploymentsAsync()
	{
		Node currentScene = GetTree().CurrentScene;
		NpcDeploymentRenderer renderer =
			currentScene?.GetNodeOrNull<NpcDeploymentRenderer>("NPCSpawnPoints");
		if (renderer != null)
			await renderer.RefreshDeploymentsAsync();
	}

	private void RemoveDeploymentSprite(int deploymentId)
	{
		Node currentScene = GetTree().CurrentScene;
		if (currentScene == null)
			return;

		foreach (Node child in currentScene.GetChildren())
		{
			if (child is AnimatedSprite2D sprite &&
				sprite.HasMeta("DeploymentId") &&
				sprite.GetMeta("DeploymentId").AsInt32() == deploymentId)
				sprite.QueueFree();
		}
	}

	private void RemoveNpcSprites(int npcId)
	{
		Node currentScene = GetTree().CurrentScene;
		if (currentScene == null)
			return;

		foreach (Node child in currentScene.GetChildren())
		{
			if (child is AnimatedSprite2D sprite &&
				sprite.HasMeta("NpcId") &&
				sprite.GetMeta("NpcId").AsInt32() == npcId)
				sprite.QueueFree();
		}
	}

	private void CloseForm()
	{
		addPopup.Hide();
		editingNpc = null;
		selectedImagePath = "";
		SelectAnimationScene("");
	}

	private void CloseSetup()
	{
		addPopup.Hide();
		configurationPopup.Hide();
		Hide();
	}

	private void ShowMessage(string text)
	{
		messageDialog.DialogText = text;
		messageDialog.PopupCentered();
	}
}
